import React, { useState, useEffect, useRef, useCallback } from 'react';
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

    // Track stats per player
    const playerStatsRef = useRef({});
    const statsUpdatedRef = useRef(false);
    const listenersRegistered = useRef(false);
    const mounted = useRef(true);
    const initializationDone = useRef(false);

    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);

    const handleGameFinished = useCallback((finalData) => {
        if (!mounted.current) return;
        if (statsUpdatedRef.current) return;

        console.log('handleGameFinished called with:', finalData);
        console.log('Current playerStatsRef:', playerStatsRef.current);

        const rawPlayers = finalData.leaderboard || finalData.players || [];

        // Calculate stats for each player from the game data
        const leaderboard = rawPlayers.map(player => {
            const playerId = player.playerId ?? player.id;
            const playerStats = playerStatsRef.current[playerId] || { correct: 0, wrong: 0 };

            console.log(`Player ${playerId} stats:`, playerStats);

            return {
                playerId: playerId,
                name: player.name,
                score: player.score ?? player.currentScore ?? 0,
                correct: playerStats.correct,
                wrong: playerStats.wrong,
            };
        }).sort((a, b) => b.score - a.score);

        const myPlayerId = parseInt(user.playerId);
        const myStats = playerStatsRef.current[myPlayerId] || { correct: 0, wrong: 0 };
        const totalQuestions = myStats.correct + myStats.wrong || finalData.totalQuestions || allQuestions.length;

        console.log('My stats:', myStats, 'Total questions:', totalQuestions);

        const processedResults = {
            players: leaderboard,
            leaderboard,
            correctCount: myStats.correct,
            wrongCount: myStats.wrong,
            totalQuestions
        };

        statsUpdatedRef.current = true;

        (async () => {
            try {
                const currentPlayerData = leaderboard.find(p => p.playerId === parseInt(user.playerId));
                const score = currentPlayerData?.score ?? 0;
                const category = gameState.settings?.category || allQuestions[0]?.category || 'General';

                await fetch('http://localhost:5216/statistics/update', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify({
                        category,
                        score,
                        correctAnswers: myStats.correct,
                        totalQuestions
                    })
                });
            } catch (error) {
                console.error('Failed to update multiplayer statistics:', error);
            }
        })();

        if (onGameFinished) {
            onGameFinished(processedResults);
        }

        setFinalResults(processedResults);
        setGameFinished(true);
    }, [allQuestions, gameState.settings?.category, user.playerId, onGameFinished]);

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
                        // Calculate stats from PlayerAnswers
                        calculatePlayerStats(gameDetails);
                        handleGameFinished(gameDetails);
                    }
                }
            } catch (err) {
                console.error('Failed to check game completion:', err);
            }
        };

        const interval = setInterval(checkGameCompletion, 1000);
        return () => clearInterval(interval);
    }, [roomCode, gameFinished, handleGameFinished]);


    // Function to calculate correct/wrong answers for all players
    const calculatePlayerStats = (gameDetails) => {
        console.log('calculatePlayerStats called with:', gameDetails);

        const questions = gameDetails.questions || [];
        const playerAnswers = gameDetails.playerAnswers || [];
        const players = gameDetails.players || [];
        const myPlayerId = parseInt(user.playerId);

        console.log('Questions:', questions);
        console.log('Player Answers:', playerAnswers);

        // Initialize stats for all players
        const stats = {};
        players.forEach(player => {
            const playerId = parseInt(player.playerId ?? player.id);

            // Preserve current player's tracked stats, initialize others
            if (playerId === myPlayerId && playerStatsRef.current[playerId]) {
                stats[playerId] = { ...playerStatsRef.current[playerId] };
                console.log(`Preserving tracked stats for current player ${playerId}:`, stats[playerId]);
            } else {
                stats[playerId] = { correct: 0, wrong: 0 };
            }
        });

        console.log('Initialized stats:', stats);

        // Calculate stats based on answers for OTHER players only
        playerAnswers.forEach(answer => {
            const playerId = parseInt(answer.playerId);

            // Skip current player - we already have their stats
            if (playerId === myPlayerId) {
                console.log(`Skipping answer calculation for current player ${playerId} - using tracked stats`);
                return;
            }

            const question = questions.find(q => q.id === answer.questionId);
            if (!question) {
                console.log('Question not found for answer:', answer);
                return;
            }

            const correctAnswers = question.correctAnswerIndexes || [];
            const selectedAnswers = answer.selectedAnswerIndexes || [];

            const sortedCorrect = [...correctAnswers].sort((a, b) => a - b);
            const sortedSelected = [...selectedAnswers].sort((a, b) => a - b);
            const isCorrect = JSON.stringify(sortedCorrect) === JSON.stringify(sortedSelected);

            console.log(`Player ${playerId} answer for Q${answer.questionId}:`, {
                selected: sortedSelected,
                correct: sortedCorrect,
                isCorrect
            });

            if (stats[playerId]) {
                if (isCorrect) {
                    stats[playerId].correct++;
                } else {
                    stats[playerId].wrong++;
                }
            }
        });

        playerStatsRef.current = stats;
        console.log('Final calculated player stats:', stats);
    };

    const loadInitialGameState = async () => {
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

                    if (gameDetails.state === 'Finished') {
                        calculatePlayerStats(gameDetails);
                        handleGameFinished(gameDetails);
                        return;
                    }

                    const updatedPlayerAnswers = {};
                    (gameDetails.players || []).forEach(player => {
                        if (player.hasSubmittedAnswer) {
                            updatedPlayerAnswers[player.playerId] = true;
                        }
                    });

                    setPlayerAnswers(updatedPlayerAnswers);

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

    useEffect(() => {
        if (initializationDone.current) {
            return;
        }
        initializationDone.current = true;

        const initializeGame = async () => {
            try {
                const token = localStorage.getItem('token');
                await signalRService.startConnection(token);

                if (signalRService.connection) {
                    signalRService.connection.onreconnected(async (connectionId) => {
                        try {
                            await signalRService.joinGameRoom(roomCode);
                        } catch (error) {
                            console.error('Failed to rejoin room after reconnection:', error);
                        }
                    });
                }

                await signalRService.joinGameRoom(roomCode);

                const nextQuestionHandler = (gameDetails) => {
                    if (!mounted.current) return;
                    console.log('NextQuestion event received');

                    setGameState(gameDetails);
                    setHasAnswered(false);
                    setPlayerAnswers({});
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

                const answerSubmittedHandler = (data) => {
                    if (!mounted.current) return;
                    console.log('AnswerSubmitted event received:', data);

                    const playerId = data.PlayerId || data.playerId;

                    setPlayerAnswers(prev => {
                        const updated = {
                            ...prev,
                            [playerId]: true
                        };
                        console.log('Updated playerAnswers:', updated);
                        return updated;
                    });
                };


                const gameFinishedHandler = async (finalData) => {
                    if (!mounted.current) return;
                    console.log('GameFinished event received (SignalR). Fetching full game details...');

                    try {
                        const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
                            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                        });

                        if (response.ok) {
                            const fullDetails = await response.json();

                            // This one has questions + playerAnswers
                            calculatePlayerStats(fullDetails);
                            handleGameFinished(fullDetails);
                            return;
                        }
                    } catch (e) {
                        console.error('Failed to fetch full game details after GameFinished:', e);
                    }

                    // Fallback (won’t have stats, but at least you show something)
                    calculatePlayerStats(finalData);
                    handleGameFinished(finalData);
                };


                const gameResetHandler = (details) => {
                    if (!mounted.current) return;
                    console.log('MultiplayerGame: GameReset event received');
                    console.log('MultiplayerGame: Cleaning up and letting App.js handle navigation');

                    // Reset all state but DON'T call onBack() - let App.js handle the navigation
                    setGameFinished(false);
                    setFinalResults(null);
                    setCurrentQuestion(null);
                    setHasAnswered(false);
                    setPlayerAnswers({});
                    setCorrectAnswerCount(0);
                    setWrongAnswerCount(0);
                    playerStatsRef.current = {};
                    statsUpdatedRef.current = false;

                    // DON'T call onBack() - App.js GameReset handler will navigate
                };

                if (!listenersRegistered.current) {
                    signalRService.onNextQuestion(nextQuestionHandler);
                    signalRService.onAnswerSubmitted(answerSubmittedHandler);
                    signalRService.onGameFinished(gameFinishedHandler);
                    signalRService.onGameReset(gameResetHandler);
                    listenersRegistered.current = true;
                }

                await loadInitialGameState();

            } catch (err) {
                console.error('Game initialization failed:', err);
            }
        };

        initializeGame();

        return () => {
            if (!mounted.current) {
                if (listenersRegistered.current) {
                    signalRService.removeListener('NextQuestion');
                    signalRService.removeListener('AnswerSubmitted');
                    signalRService.removeListener('GameFinished');
                    signalRService.removeListener('GameReset');
                    listenersRegistered.current = false;
                }
            }
        };
    }, []);

    const handleAnswerSelect = async (selectedIndex, timeLeft) => {
        if (!currentQuestion || hasAnswered) return;

        try {
            setHasAnswered(true);

            const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
            const correctAnswer = currentQuestion.correctAnswerIndexes || [];

            const sortedSelected = [...selectedIndexes].sort((a, b) => a - b);
            const sortedCorrect = [...correctAnswer].sort((a, b) => a - b);

            const isCorrect = JSON.stringify(sortedSelected) === JSON.stringify(sortedCorrect);

            const myPlayerId = parseInt(user.playerId);

            // Track current player's stats
            if (!playerStatsRef.current[myPlayerId]) {
                playerStatsRef.current[myPlayerId] = { correct: 0, wrong: 0 };
            }

            if (isCorrect) {
                playerStatsRef.current[myPlayerId].correct++;
                setCorrectAnswerCount(playerStatsRef.current[myPlayerId].correct);
                console.log('Correct answer! New count:', playerStatsRef.current[myPlayerId].correct);
            } else {
                playerStatsRef.current[myPlayerId].wrong++;
                setWrongAnswerCount(playerStatsRef.current[myPlayerId].wrong);
                console.log('Wrong answer! New count:', playerStatsRef.current[myPlayerId].wrong);
            }

            console.log('Updated playerStatsRef:', playerStatsRef.current);

            const TIME_LIMIT_SECONDS = 30;
            const clampedTime = Math.max(0, Math.min(TIME_LIMIT_SECONDS, timeLeft ?? 0));
            const questionId = currentQuestion.id || allQuestions[gameState.currentQuestionIndex]?.id;

            await fetch(`http://localhost:5216/games/${roomCode}/answer`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    questionId,
                    selectedAnswerIndexes: selectedIndexes,
                    timeLeft: clampedTime
                })
            });

            await signalRService.submitAnswer(roomCode, questionId, selectedIndexes);

            setPlayerAnswers(prev => ({
                ...prev,
                [myPlayerId]: true
            }));

        } catch (error) {
            console.error('Answer submission failed:', error);
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

    const handleBackToLobby = () => {
        onBack();
    };

    const answeredPlayersCount = Object.values(playerAnswers).filter(v => v === true).length;
    const totalPlayersCount = players.length;
    const currentQuestionIndex = gameState.currentQuestionIndex || 0;
    const totalQuestions = allQuestions.length;
    const isLastQuestion = currentQuestionIndex >= totalQuestions - 1;
    const currentPlayer = players.find(p => p.playerId === parseInt(user.playerId));
    const currentScore = currentPlayer?.score ?? currentPlayer?.currentScore ?? 0;

    if (gameFinished && finalResults) {
        return (
            <MultiplayerResults
                finalResults={finalResults}
                onBackToLobby={handleBackToLobby}
                onPlayAgain={() => { }}
                roomCode={roomCode}
                isHost={isHost}
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
                        {players.map((player) => {
                            const playerScore = player.score ?? player.currentScore ?? 0;
                            const hasAnswered = playerAnswers[player.playerId] === true;

                            return (
                                <div key={player.playerId} className="player-status">
                                    <div className="player-avatar-small">
                                        {player.name?.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="player-info">
                                        <div className="player-name-row">
                                            <span className="player-name">{player.name}</span>
                                            {player.playerId === parseInt(user.playerId) && (
                                                <span className="you-badge">You</span>
                                            )}
                                        </div>
                                        <div className="player-score">
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                                                <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                                            </svg>
                                            <span>{playerScore} pts</span>
                                        </div>
                                    </div>
                                    <div className={`status-dot ${hasAnswered ? 'answered' : 'waiting'}`}></div>
                                </div>
                            );
                        })}
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
                        currentScore={currentScore}
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
