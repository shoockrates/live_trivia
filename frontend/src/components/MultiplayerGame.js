import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGame.css';
import signalRService from '../services/signalRService';
import QuestionDisplay from './QuestionDisplay';
import MultiplayerResults from './MultiplayerResults';

const MultiplayerGame = ({ roomCode, user, onGameFinished, onBack }) => {
    const [gameState, setGameState] = useState({});
    const [currentQuestion, setCurrentQuestion] = useState(null);
    const [players, setPlayers] = useState([]);
    const [playerAnswers, setPlayerAnswers] = useState({});
    const [gameFinished, setGameFinished] = useState(false);
    const [hasAnswered, setHasAnswered] = useState(false);
    const [isHost, setIsHost] = useState(false);
    const [allQuestions, setAllQuestions] = useState([]);
    const [finalResults, setFinalResults] = useState(null);
    const [correctAnswerCount, setCorrectAnswerCount] = useState(0);
    const [wrongAnswerCount, setWrongAnswerCount] = useState(0);

    const listenersRegistered = useRef(false);
    const mounted = useRef(true);

    useEffect(() => {
        mounted.current = true;
        return () => { mounted.current = false; };
    }, []);

    // Game finished handler - moved to component scope so it can be reused
    const handleGameFinished = (finalData) => {
        if (!mounted.current) return;
        console.log('Game finished handler called with data:', finalData);

        // The server sends the complete game state, extract player statistics
        const players = finalData.players || [];

        // Create leaderboard from player data
        const leaderboard = players.map(player => ({
            playerId: player.playerId,
            name: player.name,
            score: player.score || 0,
            correct: player.correctAnswers || 0,
            wrong: (player.totalAnswers || 0) - (player.correctAnswers || 0)
        })).sort((a, b) => b.score - a.score);

        // Create the final results structure that MultiplayerResults expects
        const processedResults = {
            players: leaderboard,
            leaderboard: leaderboard,
            correctCount: finalData.correctCount || 0,
            wrongCount: finalData.wrongCount || 0,
            totalQuestions: finalData.totalQuestions || allQuestions.length
        };

        console.log('Processed results for MultiplayerResults:', processedResults);

        // Update statistics for all players
        updateMultiplayerStatistics(processedResults);

        // Also notify parent component
        if (onGameFinished) {
            onGameFinished(processedResults);
        }

        setFinalResults(processedResults);
        setGameFinished(true);
    };
    // Add this useEffect to debug what's happening with other players
    useEffect(() => {
        console.log('MultiplayerGame state update:', {
            playerId: user.playerId,
            gameFinished,
            hasFinalResults: !!finalResults,
            currentQuestionIndex: gameState.currentQuestionIndex,
            totalQuestions: allQuestions.length,
            isHost,
            playersCount: players.length
        });
    }, [gameFinished, finalResults, gameState.currentQuestionIndex, allQuestions.length, isHost, players.length, user.playerId]);

    // Effect to periodically check if game is finished
    // Replace the current game completion check useEffect with this:
    useEffect(() => {
        const checkGameCompletion = async () => {
            if (gameFinished) return;

            try {
                const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
                    headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                });
                if (response.ok) {
                    const gameDetails = await response.json();

                    if (gameDetails.state === 'Finished' && !gameFinished) {
                        console.log(`Game finished detected by player ${user.playerId} via state check`);
                        handleGameFinished(gameDetails);
                    }
                }
            } catch (err) {
                console.error('Failed to check game completion:', err);
            }
        };

        // Check every second for all players
        const interval = setInterval(checkGameCompletion, 1000);
        return () => clearInterval(interval);
    }, [roomCode, gameFinished, user.playerId]);

    useEffect(() => {
        let nextQuestionHandler, answerSubmittedHandler, gameFinishedHandler;

        // Add this helper function
        const processFinalGameData = (finalData) => {
            console.log('Processing final game data for player:', user.playerId);

            // The server sends the complete game state, extract player statistics
            const players = finalData.players || [];

            // Create leaderboard from player data
            const leaderboard = players.map(player => ({
                playerId: player.playerId,
                name: player.name,
                score: player.score || 0,
                correct: player.correctAnswers || 0,
                wrong: (player.totalAnswers || 0) - (player.correctAnswers || 0)
            })).sort((a, b) => b.score - a.score);

            // Create the final results structure that MultiplayerResults expects
            const processedResults = {
                players: leaderboard,
                leaderboard: leaderboard,
                correctCount: finalData.correctCount || 0,
                wrongCount: finalData.wrongCount || 0,
                totalQuestions: finalData.totalQuestions || allQuestions.length
            };

            console.log('Processed results for player:', user.playerId, processedResults);

            // Update statistics for all players
            updateMultiplayerStatistics(processedResults);

            // Also notify parent component
            if (onGameFinished) {
                onGameFinished(processedResults);
            }

            setFinalResults(processedResults);
            setGameFinished(true);
        };

        const initializeGame = async () => {
            try {
                const token = localStorage.getItem('token');
                await signalRService.startConnection(token);

                nextQuestionHandler = (gameDetails) => {
                    if (!mounted.current) return;
                    console.log('Next question received:', gameDetails);

                    setGameState(gameDetails);
                    setHasAnswered(false);
                    setPlayerAnswers({});
                    setPlayers(gameDetails.players || []);
                    setAllQuestions(gameDetails.questions || []);


                    // Check if this is the last question
                    const currentIndex = gameDetails.currentQuestionIndex || 0;
                    const totalQuestions = gameDetails.questions?.length || 0;
                    const isLastQuestion = currentIndex >= totalQuestions - 1;

                    if (isLastQuestion) {
                        console.log('Last question reached, next action will finish game');
                    }

                    if (gameDetails.currentQuestionIndex >= 0 && gameDetails.questions?.length > 0) {
                        const q = gameDetails.questions[gameDetails.currentQuestionIndex];
                        setCurrentQuestion({
                            text: q.text,
                            answers: q.answers,
                            id: q.id,
                            correctAnswerIndexes: q.correctAnswerIndexes || []
                        });
                    }
                };

                answerSubmittedHandler = (data) => {
                    if (!mounted.current) return;
                    console.log('Answer submitted by player:', data);

                    setPlayerAnswers(prev => ({
                        ...prev,
                        [data.PlayerId || data.playerId]: true
                    }));
                };

                // In the main useEffect, make sure the GameFinished handler is robust:
                gameFinishedHandler = (finalData) => {
                    console.log('GameFinished SignalR event received by player:', user.playerId, finalData);
                    if (!mounted.current) return;

                    // Process immediately when we get the event
                    const players = finalData.players || [];
                    const leaderboard = players.map(player => ({
                        playerId: player.playerId,
                        name: player.name,
                        score: player.score || 0,
                        correct: player.correctAnswers || 0,
                        wrong: (player.totalAnswers || 0) - (player.correctAnswers || 0)
                    })).sort((a, b) => b.score - a.score);

                    const processedResults = {
                        players: leaderboard,
                        leaderboard: leaderboard,
                        correctCount: finalData.correctCount || 0,
                        wrongCount: finalData.wrongCount || 0,
                        totalQuestions: finalData.totalQuestions || allQuestions.length
                    };

                    console.log('Processed results from SignalR event:', processedResults);

                    setFinalResults(processedResults);
                    setGameFinished(true);

                    // Update statistics
                    updateMultiplayerStatistics(processedResults);

                    if (onGameFinished) {
                        onGameFinished(processedResults);
                    }
                };

                if (!listenersRegistered.current) {
                    signalRService.onNextQuestion(nextQuestionHandler);
                    signalRService.onAnswerSubmitted(answerSubmittedHandler);
                    signalRService.onGameFinished(gameFinishedHandler);
                    listenersRegistered.current = true;
                }

                await signalRService.joinGameRoom(roomCode);
                await loadGameState();

            } catch (err) {
                console.error('Game initialization failed:', err);
            }
        };

        const loadGameState = async () => {
            try {
                const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
                    headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                });
                if (response.ok) {
                    const gameDetails = await response.json();
                    if (mounted.current) {
                        setGameState(gameDetails);
                        setPlayers(gameDetails.players || []);
                        setIsHost(gameDetails.hostPlayerId === parseInt(user.playerId));
                        setAllQuestions(gameDetails.questions || []);

                        // Check if game is already finished
                        if (gameDetails.state === 'Finished') {
                            console.log('Game already finished when loading state');
                            handleGameFinished(gameDetails); // Use the component-scoped function
                            return;
                        }

                        // Initialize player answers based on current game state
                        const initialPlayerAnswers = {};
                        if (gameDetails.playerAnswers) {
                            Object.keys(gameDetails.playerAnswers).forEach(playerId => {
                                initialPlayerAnswers[playerId] = true;
                            });
                        }
                        setPlayerAnswers(initialPlayerAnswers);

                        if (gameDetails.state === 'InProgress' && gameDetails.currentQuestionIndex >= 0) {
                            const q = gameDetails.questions?.[gameDetails.currentQuestionIndex];
                            if (q) {
                                setCurrentQuestion({
                                    text: q.text,
                                    answers: q.answers,
                                    id: q.id,
                                    correctAnswerIndexes: q.correctAnswerIndexes || []
                                });
                            }
                        }
                    }
                }
            } catch (err) {
                console.error('Load game state failed:', err);
            }
        };

        initializeGame();

        return () => {
            if (listenersRegistered.current) {
                signalRService.removeListener('NextQuestion', nextQuestionHandler);
                signalRService.removeListener('AnswerSubmitted', answerSubmittedHandler);
                signalRService.removeListener('GameFinished', gameFinishedHandler);
                listenersRegistered.current = false;
            }
            signalRService.leaveGameRoom(roomCode).catch(() => { });
        };
    }, [roomCode, user.playerId, onGameFinished]);

    const updateMultiplayerStatistics = async (finalData) => {
        try {
            // Get current player's results from final data
            const playerResults = finalData.leaderboard?.find(p =>
                p.playerId === parseInt(user.playerId)
            );

            if (playerResults) {
                const totalQuestions = allQuestions.length;
                const correctAnswers = playerResults.correct || 0;
                const wrongAnswers = playerResults.wrong || 0;
                const score = playerResults.score || 0;

                // Determine category from game settings or questions
                const category = gameState.settings?.category ||
                    allQuestions[0]?.category ||
                    'General';

                console.log('Updating multiplayer stats:', {
                    category,
                    score,
                    correctAnswers,
                    totalQuestions
                });

                // Call the statistics update endpoint
                await fetch(`http://localhost:5216/statistics/update`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify({
                        category: category,
                        score: score,
                        correctAnswers: correctAnswers,
                        totalQuestions: totalQuestions
                    })
                });

                console.log('Multiplayer statistics updated successfully');
            }
        } catch (error) {
            console.error('Failed to update multiplayer statistics:', error);
        }
    };

    const handleAnswerSelect = async (selectedIndex) => {
        if (!currentQuestion || hasAnswered) return;

        try {
            setHasAnswered(true);

            const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
            const correctAnswer = currentQuestion.correctAnswerIndexes || [];

            // Check if answer is correct
            const isCorrect = JSON.stringify([...selectedIndexes].sort()) ===
                JSON.stringify([...correctAnswer].sort());

            // Update counts
            if (isCorrect) {
                setCorrectAnswerCount(prev => prev + 1);
            } else {
                setWrongAnswerCount(prev => prev + 1);
            }

            const questionId = currentQuestion.id || allQuestions[gameState.currentQuestionIndex]?.id;

            await fetch(`http://localhost:5216/games/${roomCode}/answer`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({ questionId, selectedAnswerIndexes: selectedIndexes })
            });

            await signalRService.submitAnswer(roomCode, questionId, selectedIndexes);

            setPlayerAnswers(prev => ({
                ...prev,
                [user.playerId]: true
            }));

        } catch (error) {
            console.error('Answer submission failed:', error);
            setHasAnswered(false);
        }
    };

    const handleNextQuestion = async () => {
        if (!isHost) return;

        try {
            const currentIndex = gameState.currentQuestionIndex || 0;
            const totalQuestions = allQuestions.length;
            const isLastQuestion = currentIndex >= totalQuestions - 1;

            if (isLastQuestion) {
                console.log('Finishing game by calling next question on last question');

                // Call next question endpoint even on last question - this should trigger game finished state
                await fetch(`http://localhost:5216/games/${roomCode}/next`, {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                });

                console.log('Next question called on final question - game should finish');
            } else {
                console.log('Calling next question endpoint...');
                await fetch(`http://localhost:5216/games/${roomCode}/next`, {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                });
            }

        } catch (err) {
            console.error('Next question/finish game failed:', err);
        }
    };

    const handlePlayAgain = () => {
        // Reset game state and wait for host to start new game
        setGameFinished(false);
        setFinalResults(null);
        setCurrentQuestion(null);
        setHasAnswered(false);
        setPlayerAnswers({});
        setCorrectAnswerCount(0);
        setWrongAnswerCount(0);

        console.log('Play again requested');
    };

    const handleBackToLobby = () => {
        onBack();
    };

    const answeredPlayersCount = Object.values(playerAnswers).filter(v => v === true).length;
    const totalPlayersCount = players.length;
    const currentQuestionIndex = gameState.currentQuestionIndex || 0;
    const totalQuestions = allQuestions.length;

    // Check if this is the last question
    const isLastQuestion = currentQuestionIndex >= totalQuestions - 1;

    // Show results screen when game is finished
    if (gameFinished && finalResults) {
        console.log('Rendering MultiplayerResults with:', finalResults);
        return (
            <MultiplayerResults
                finalResults={finalResults}
                onBackToLobby={handleBackToLobby}
                onPlayAgain={handlePlayAgain}
                roomCode={roomCode}
            />
        );
    }

    return (
        <div className="multiplayer-game-container">
            <div className="multiplayer-game-card">
                <div className="game-header">
                    <h2 className="game-title">Multiplayer Game</h2>
                    <div className="room-info-section">
                        <span className="room-code-label">Room Code</span>
                        <div className="room-code-display">
                            <span className="room-code">{roomCode}</span>
                        </div>
                    </div>
                </div>

                <div className="players-status">
                    <h4>Players ({totalPlayersCount})</h4>
                    <div className="players-grid">
                        {players.map((player) => (
                            <div key={player.playerId} className="player-status">
                                <div className="player-avatar-small">
                                    {player.name?.charAt(0).toUpperCase()}
                                </div>
                                <div className="player-info">
                                    <span className="player-name">{player.name}</span>
                                    {player.playerId === parseInt(user.playerId) && (
                                        <span className="you-badge">You</span>
                                    )}
                                </div>
                                <div className={`status-dot ${playerAnswers[player.playerId] ? 'answered' : 'waiting'}`}></div>
                            </div>
                        ))}
                    </div>
                    <div className="answer-progress">
                        {answeredPlayersCount} / {totalPlayersCount} answered
                        {isLastQuestion && <span className="last-question-indicator"> • Final Question</span>}
                    </div>
                </div>

                {currentQuestion ? (
                    <QuestionDisplay
                        question={currentQuestion.text}
                        answers={currentQuestion.answers}
                        correctIndexes={currentQuestion.correctAnswerIndexes || []}
                        onAnswerSelect={handleAnswerSelect}
                        onNext={handleNextQuestion}
                        currentIndex={currentQuestionIndex}
                        totalQuestions={totalQuestions}
                        correctCount={correctAnswerCount}
                        wrongCount={wrongAnswerCount}
                        revealed={hasAnswered}
                        questionIn={true}
                        isMultiplayer={true}
                        isHost={isHost}
                        answeredPlayers={answeredPlayersCount}
                        totalPlayers={totalPlayersCount}
                        isLastQuestion={isLastQuestion}
                    />
                ) : (
                    <div className="waiting-for-question">
                        <div className="loading-spinner"></div>
                        <p>Waiting for game to start...</p>
                    </div>
                )}

                <button className="back-button" onClick={handleBackToLobby}>
                    Leave Game
                </button>
            </div>
        </div>
    );
};

export default MultiplayerGame;
