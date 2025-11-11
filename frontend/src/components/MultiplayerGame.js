import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGame.css';
import signalRService from '../services/signalRService';
import QuestionDisplay from './QuestionDisplay';

const MultiplayerGame = ({ roomCode, user, onGameFinished, onBack }) => {
  const [gameState, setGameState] = useState({});
  const [currentQuestion, setCurrentQuestion] = useState(null);
  const [players, setPlayers] = useState([]);
  const [playerAnswers, setPlayerAnswers] = useState({});
  const [gameFinished, setGameFinished] = useState(false);
  const [hasAnswered, setHasAnswered] = useState(false);
  const [isHost, setIsHost] = useState(false);
  
  const listenersRegistered = useRef(false);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    
    return () => {
      mounted.current = false;
    };
  }, []);

  useEffect(() => {
    let nextQuestionHandler;
    let answerSubmittedHandler;
    let gameFinishedHandler;

    const initializeGame = async () => {
      try {
        const token = localStorage.getItem('token');
        await signalRService.startConnection(token);
        
        // Define event handlers
        nextQuestionHandler = (gameDetails) => {
          console.log('Next question event:', gameDetails);
          if (!mounted.current) return;
          
          setGameState(gameDetails);
          setHasAnswered(false);
          setPlayerAnswers({}); // Reset answers for new question
          
          if (gameDetails.currentQuestionIndex >= 0 && gameDetails.questions) {
            const question = gameDetails.questions[gameDetails.currentQuestionIndex];
            setCurrentQuestion({
              text: question?.text || gameDetails.currentQuestionText,
              answers: question?.answers || gameDetails.currentQuestionAnswers,
              id: question?.id,
              correctAnswerIndexes: question?.correctAnswerIndexes || []
            });
          }
        };

        answerSubmittedHandler = (data) => {
          console.log('Answer submitted event:', data);
          if (!mounted.current) return;
          
          setPlayerAnswers(prev => ({
            ...prev,
            [data.PlayerId]: { 
              submitted: true, 
              timestamp: data.Timestamp,
              playerName: data.PlayerName 
            }
          }));
        };

        gameFinishedHandler = (finalData) => {
          console.log('Game finished event:', finalData);
          if (!mounted.current) return;
          
          setGameFinished(true);
          onGameFinished(finalData);
        };

        // Register listeners only once
        if (!listenersRegistered.current) {
          signalRService.onNextQuestion(nextQuestionHandler);
          signalRService.onAnswerSubmitted(answerSubmittedHandler);
          signalRService.onGameFinished(gameFinishedHandler);
          listenersRegistered.current = true;
        }

        // Join the room
        await signalRService.joinGameRoom(roomCode);

        // Load initial game state
        await loadGameState();

      } catch (error) {
        console.error('Failed to initialize game:', error);
      }
    };

    const loadGameState = async () => {
      try {
        const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        
        if (response.ok) {
          const gameDetails = await response.json();
          if (mounted.current) {
            setGameState(gameDetails);
            setPlayers(gameDetails.players || []);
            setIsHost(gameDetails.hostPlayerId === parseInt(user.playerId));
            
            // Set current question if game is in progress
            if (gameDetails.state === 'InProgress' && 
                gameDetails.currentQuestionIndex >= 0 && 
                gameDetails.questions) {
              const question = gameDetails.questions[gameDetails.currentQuestionIndex];
              setCurrentQuestion({
                text: question?.text,
                answers: question?.answers,
                id: question?.id,
                correctAnswerIndexes: question?.correctAnswerIndexes || []
              });
            }
          }
        }
      } catch (error) {
        console.error('Failed to load game state:', error);
      }
    };

    initializeGame();

    // Cleanup function
    return () => {
      if (listenersRegistered.current) {
        if (nextQuestionHandler) {
          signalRService.removeListener('NextQuestion', nextQuestionHandler);
        }
        if (answerSubmittedHandler) {
          signalRService.removeListener('AnswerSubmitted', answerSubmittedHandler);
        }
        if (gameFinishedHandler) {
          signalRService.removeListener('GameFinished', gameFinishedHandler);
        }
        
        listenersRegistered.current = false;
      }
      
      // Leave the room
      signalRService.leaveGameRoom(roomCode).catch(err => 
        console.error('Error leaving room:', err)
      );
    };
  }, [roomCode, user.playerId, onGameFinished]);

  const handleAnswerSelect = async (selectedIndex) => {
    if (!currentQuestion || hasAnswered) return;

    try {
      setHasAnswered(true);
      
      const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
      
      // Submit to API for persistence
      const response = await fetch(`http://localhost:5216/games/${roomCode}/answer`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          questionId: currentQuestion.id,
          selectedAnswerIndexes: selectedIndexes
        })
      });

      if (!response.ok) {
        throw new Error('Failed to submit answer to API');
      }

      // Also notify via SignalR for real-time updates
      await signalRService.submitAnswer(roomCode, currentQuestion.id, selectedIndexes);
      
      console.log('Answer submitted successfully');
    } catch (error) {
      console.error('Failed to submit answer:', error);
      setHasAnswered(false); // Allow retry
    }
  };

  const handleNextQuestion = async () => {
    if (!isHost) {
      alert('Only the host can advance to the next question');
      return;
    }

    try {
      const response = await fetch(`http://localhost:5216/games/${roomCode}/next`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to advance to next question');
      }

      const result = await response.json();
      console.log('Next question result:', result);
      
      // The SignalR event will handle updating the UI
    } catch (error) {
      console.error('Failed to advance to next question:', error);
      alert('Failed to advance: ' + error.message);
    }
  };

  const handleBack = () => {
    // Cleanup will happen in useEffect cleanup
    onBack();
  };

  if (gameFinished) {
    return (
      <div className="multiplayer-game-container">
        <div className="game-finished">
          <h2>🎉 Game Finished!</h2>
          <p>Calculating final results...</p>
          <button className="back-button" onClick={handleBack}>
            Return to Lobby
          </button>
        </div>
      </div>
    );
  }

  const answeredCount = Object.keys(playerAnswers).length;
  const allAnswered = answeredCount >= players.length;

  return (
    <div className="multiplayer-game-container">
      <div className="game-header">
        <div className="room-info">
          <h2>Room: {roomCode}</h2>
          {isHost && <span className="host-indicator">👑 Host</span>}
        </div>
        
        <div className="players-status">
          <h4>Players ({players.length})</h4>
          <div className="players-grid">
            {players.map((player, index) => {
              const hasAnsweredQuestion = playerAnswers[player.connectionId] || 
                                         playerAnswers[player.playerId];
              
              return (
                <div key={player.playerId || index} className="player-status">
                  <div className="player-avatar-small">
                    {(player.name?.charAt(0) || player.username?.charAt(0) || 'P').toUpperCase()}
                  </div>
                  <div className="player-info">
                    <span className="player-name">{player.name || player.username}</span>
                    <div className={`status-dot ${hasAnsweredQuestion ? 'answered' : 'waiting'}`} 
                         title={hasAnsweredQuestion ? 'Answered' : 'Waiting'} />
                  </div>
                </div>
              );
            })}
          </div>
          
          <div className="answer-progress">
            {answeredCount} / {players.length} answered
            {isHost && allAnswered && (
              <button 
                className="next-question-btn"
                onClick={handleNextQuestion}
              >
                Next Question →
              </button>
            )}
          </div>
        </div>
      </div>

      {currentQuestion ? (
        <QuestionDisplay
          question={currentQuestion.text}
          answers={currentQuestion.answers}
          correctIndexes={currentQuestion.correctAnswerIndexes}
          onAnswerSelect={handleAnswerSelect}
          onNext={handleNextQuestion}
          currentIndex={gameState.currentQuestionIndex || 0}
          totalQuestions={gameState.totalQuestions || gameState.questions?.length || 0}
          correctCount={0}
          wrongCount={0}
          revealed={hasAnswered}
          isMultiplayer={true}
          isHost={isHost}
          hasAnswered={hasAnswered}
          answeredPlayers={answeredCount}
          totalPlayers={players.length}
        />
      ) : (
        <div className="waiting-for-question">
          <div className="loading-spinner"></div>
          <p>Waiting for game to start...</p>
        </div>
      )}

      <button className="leave-game-button" onClick={handleBack}>
        Leave Game
      </button>
    </div>
  );
};

export default MultiplayerGame;
