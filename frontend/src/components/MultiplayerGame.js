import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGame.css';
import signalRService from '../services/signalRService';
import QuestionDisplay from './QuestionDisplay';

const MultiplayerGame = ({ roomCode, user, onGameFinished, onBack }) => {
    const [gameState, setGameState] = useState({});
    const [currentQuestion, setCurrentQuestion] = useState(null);
    const [players, setPlayers] = useState([]);
    const [playerAnswers, setPlayerAnswers] = useState({}); // { playerId: true } or { playerId: { submitted: true, ... } }
    const [gameFinished, setGameFinished] = useState(false);
    const [hasAnswered, setHasAnswered] = useState(false);
    const [isHost, setIsHost] = useState(false);
    const [allQuestions, setAllQuestions] = useState([]);

    const listenersRegistered = useRef(false);
    const mounted = useRef(true);

    useEffect(() => {
        mounted.current = true;
        return () => { mounted.current = false; };
    }, []);

    useEffect(() => {
        let nextQuestionHandler, answerSubmittedHandler, gameFinishedHandler;

        const initializeGame = async () => {
            try {
                const token = localStorage.getItem('token');
                await signalRService.startConnection(token);

                nextQuestionHandler = (gameDetails) => {
                    if (!mounted.current) return;
                    console.log('Next question:', gameDetails);

                    setGameState(gameDetails);
                    setHasAnswered(false);
                    setPlayerAnswers({}); // Reset answers for new question
                    setPlayers(gameDetails.players || []);
                    setAllQuestions(gameDetails.questions || []);

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
                    console.log('Answer submitted:', data);

                    setPlayerAnswers(prev => ({
                        ...prev,
                        [data.PlayerId]: true  // ← We only care about "true" now
                    }));
                };

                gameFinishedHandler = (finalData) => {
                    if (!mounted.current) return;
                    setGameFinished(true);
                    onGameFinished(finalData);
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
                console.error('Init failed:', err);
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
                console.error('Load state failed:', err);
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

    const handleAnswerSelect = async (selectedIndex) => {
        if (!currentQuestion || hasAnswered) return;

        try {
            setHasAnswered(true);

            const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
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

            // Mark yourself as answered immediately
            setPlayerAnswers(prev => ({
                ...prev,
                [user.playerId]: true
            }));

        } catch (error) {
            console.error('Answer failed:', error);
            setHasAnswered(false);
        }
    };

    const handleNextQuestion = async () => {
        if (!isHost) return;
        try {
            await fetch(`http://localhost:5216/games/${roomCode}/next`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
        } catch (err) {
            console.error('Next question failed:', err);
        }
    };

    // CORRECT COUNT — this was the bug!
    const answeredPlayersCount = Object.values(playerAnswers).filter(v => v === true).length;
    const totalPlayersCount = players.length;
    const allAnswered = answeredPlayersCount >= totalPlayersCount;

    if (gameFinished) {
        return (
            <div className="multiplayer-game-container">
                <div className="game-finished">
                    <h2>Game Finished!</h2>
                    <p>Calculating final results...</p>
                    <button className="back-button" onClick={onBack}>Return to Lobby</button>
                </div>
            </div>
        );
    }

    return (
        <div className="multiplayer-game-container">
            {/* Header */}
            <div className="game-header">
                <div className="room-info">
                    <h2>Room: {roomCode}</h2>
                    {isHost && <span className="host-indicator">Host</span>}
                </div>

                <div className="players-status">
                    <h4>Players ({totalPlayersCount})</h4>
                    <div className="answer-progress">
                        {answeredPlayersCount} / {totalPlayersCount} answered
                    </div>
                </div>
            </div>

            {/* Main Question */}
            {currentQuestion ? (
                <QuestionDisplay
                    question={currentQuestion.text}
                    answers={currentQuestion.answers}
                    correctIndexes={currentQuestion.correctAnswerIndexes || []}
                    onAnswerSelect={handleAnswerSelect}
                    onNext={handleNextQuestion}
                    currentIndex={gameState.currentQuestionIndex || 0}
                    totalQuestions={allQuestions.length || 5}
                    correctCount={0}
                    wrongCount={0}
                    revealed={hasAnswered}
                    questionIn={true}

                    // MULTIPLAYER PROPS — NOW 100% CORRECT
                    isMultiplayer={true}
                    isHost={isHost}
                    answeredPlayers={answeredPlayersCount}
                    totalPlayers={totalPlayersCount}
                />
            ) : (
                <div className="waiting-for-question">
                    <div className="loading-spinner"></div>
                    <p>Waiting for game to start...</p>
                </div>
            )}

            <button className="leave-game-button" onClick={onBack}>
                Leave Game
            </button>
        </div>
    );
};

export default MultiplayerGame;
