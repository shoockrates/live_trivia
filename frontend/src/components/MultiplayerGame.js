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
    const timerExpireHandledRef = useRef(false);

    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);

    // Calculate stats for ALL players from server data
    const calculateAllPlayerStats = useCallback((gameDetails) => {
        console.log('calculateAllPlayerStats called');

        const questions = gameDetails.questions || [];
        const playerAnswers = gameDetails.playerAnswers || [];
        const players = gameDetails.players || [];

        console.log('Questions:', questions.length);
        console.log('Player Answers:', playerAnswers.length);
        console.log('Players:', players.length);

        // Initialize stats for all players
        const stats = {};
        players.forEach(player => {
            const playerId = parseInt(player.playerId ?? player.id);
            stats[playerId] = { correct: 0, wrong: 0 };
        });

        // Group answers by player
        const answersByPlayer = {};
        playerAnswers.forEach(answer => {
            const playerId = parseInt(answer.playerId);
            if (!answersByPlayer[playerId]) {
                answersByPlayer[playerId] = [];
            }
            answersByPlayer[playerId].push(answer);
        });

        // Calculate stats for each player
        players.forEach(player => {
            const playerId = parseInt(player.playerId ?? player.id);
            const playerAnswersList = answersByPlayer[playerId] || [];

            console.log(`Player ${playerId} has ${playerAnswersList.length} answers out of ${questions.length} questions`);

            // Check each answer
            playerAnswersList.forEach(answer => {
                const question = questions.find(q => q.id === answer.questionId);
                if (!question) {
                    console.log(`Question not found for answer:`, answer);
                    return;
                }

                const correctAnswers = question.correctAnswerIndexes || [];
                const selectedAnswers = answer.selectedAnswerIndexes || [];

                const sortedCorrect = [...correctAnswers].sort((a, b) => a - b);
                const sortedSelected = [...selectedAnswers].sort((a, b) => a - b);
                const isCorrect = JSON.stringify(sortedCorrect) === JSON.stringify(sortedSelected);

                if (isCorrect) {
                    stats[playerId].correct++;
                } else {
                    stats[playerId].wrong++;
                }
            });

            // Mark unanswered questions as wrong
            const unanswered = questions.length - playerAnswersList.length;
            if (unanswered > 0) {
                console.log(`Player ${playerId} has ${unanswered} unanswered questions`);
                stats[playerId].wrong += unanswered;
            }
        });

        console.log('Final calculated stats for all players:', stats);
        return stats;
    }, []);

    const handleGameFinished = useCallback((finalData) => {
        if (!mounted.current) return;
        if (statsUpdatedRef.current) return;

        console.log('handleGameFinished called');

        const rawPlayers = finalData.leaderboard || finalData.players || [];

        // Calculate stats from server data
        const stats = calculateAllPlayerStats(finalData);

        const leaderboard = rawPlayers.map(player => {
            const playerId = parseInt(player.playerId ?? player.id);
            const playerStats = stats[playerId] || { correct: 0, wrong: 0 };

            console.log(`Player ${playerId} (${player.name}) final stats:`, playerStats);

            return {
                playerId: playerId,
                name: player.name,
                score: player.score ?? player.currentScore ?? 0,
                correct: playerStats.correct,
                wrong: playerStats.wrong,
            };
        }).sort((a, b) => b.score - a.score);

        const myPlayerId = parseInt(user.playerId);
        const myStats = stats[myPlayerId] || { correct: 0, wrong: 0 };
        const totalQuestions = myStats.correct + myStats.wrong || finalData.totalQuestions || allQuestions.length;

        console.log('My final stats:', myStats, 'Total questions:', totalQuestions);

        const processedResults = {
            players: leaderboard,
            leaderboard,
            correctCount: myStats.correct,
            wrongCount: myStats.wrong,
            totalQuestions
        };

        statsUpdatedRef.current = true;

        // Update backend statistics
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
    }, [allQuestions, gameState.settings?.category, user.playerId, onGameFinished, calculateAllPlayerStats]);

    // Check for game completion periodically
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

    // Poll for answer status updates
    useEffect(() => {
        if (gameFinished || !currentQuestion) return;

        const syncAnswerStatus = async () => {
            try {
                const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
                    headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                });

                if (response.ok) {
                    const gameDetails = await response.json();
                    const players = gameDetails.players || [];

                    setPlayerAnswers(prev => {
                        const updated = { ...prev };

                        players.forEach(player => {
                            const playerId = parseInt(player.playerId);
                            if (player.hasSubmittedAnswer) {
                                updated[playerId] = true;
                            }
                        });

                        return updated;
                    });
                }
            } catch (err) {
                console.error('Failed to sync answer status:', err);
            }
        };

        const interval = setInterval(syncAnswerStatus, 500);

        return () => clearInterval(interval);
    }, [roomCode, currentQuestion, gameFinished]);

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

                    timerExpireHandledRef.current = false;

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

                    const rawId = data.PlayerId ?? data.playerId ?? data.playerid;
                    const playerId = parseInt(rawId, 10);

                    console.log('🔔 AnswerSubmitted SignalR event received:', {
                        raw: data,
                        playerId: playerId
                    });

                    if (isNaN(playerId)) {
                        console.error('Invalid playerId in AnswerSubmitted event:', data);
                        return;
                    }

                    setPlayerAnswers(prev => {
                        const updated = {
                            ...prev,
                            [playerId]: true
                        };
                        console.log('📝 Updated playerAnswers after SignalR:', updated);
                        return updated;
                    });
                };

                const gameFinishedHandler = async (finalData) => {
                    if (!mounted.current) return;
                    console.log('GameFinished event received. Fetching full game details...');

                    try {
                        const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
                            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
                        });

                        if (response.ok) {
                            const fullDetails = await response.json();
                            handleGameFinished(fullDetails);
                            return;
                        }
                    } catch (e) {
                        console.error('Failed to fetch full game details:', e);
                    }

                    handleGameFinished(finalData);
                };

                const gameResetHandler = (details) => {
                    if (!mounted.current) return;
                    console.log('MultiplayerGame: GameReset event received');

                    setGameFinished(false);
                    setFinalResults(null);
                    setCurrentQuestion(null);
                    setHasAnswered(false);
                    setPlayerAnswers({});
                    setCorrectAnswerCount(0);
                    setWrongAnswerCount(0);
                    playerStatsRef.current = {};
                    statsUpdatedRef.current = false;
                    timerExpireHandledRef.current = false;
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
    }, [roomCode, user.playerId, handleGameFinished]);

    // Debug playerAnswers changes
    useEffect(() => {
        const count = Object.keys(playerAnswers).filter(
            id => playerAnswers[id] === true
        ).length;
        console.log('PlayerAnswers changed:', playerAnswers, 'Count:', count);
    }, [playerAnswers]);

    const handleAnswerSelect = async (selectedIndex, timeLeft) => {
        if (!currentQuestion || hasAnswered) {
            console.log('Answer select blocked - already answered or no question');
            return;
        }

        try {
            console.log('handleAnswerSelect called with:', selectedIndex, 'timeLeft:', timeLeft);

            const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
            const correctAnswer = currentQuestion.correctAnswerIndexes || [];

            const sortedSelected = [...selectedIndexes].sort((a, b) => a - b);
            const sortedCorrect = [...correctAnswer].sort((a, b) => a - b);

            const isCorrect = JSON.stringify(sortedSelected) === JSON.stringify(sortedCorrect);

            const myPlayerId = parseInt(user.playerId);

            // Track current player's stats locally
            if (!playerStatsRef.current[myPlayerId]) {
                playerStatsRef.current[myPlayerId] = { correct: 0, wrong: 0 };
            }

            if (isCorrect) {
                playerStatsRef.current[myPlayerId].correct++;
                setCorrectAnswerCount(playerStatsRef.current[myPlayerId].correct);
                console.log('✓ Correct! Total correct:', playerStatsRef.current[myPlayerId].correct);
            } else {
                playerStatsRef.current[myPlayerId].wrong++;
                setWrongAnswerCount(playerStatsRef.current[myPlayerId].wrong);
                console.log('✗ Wrong! Total wrong:', playerStatsRef.current[myPlayerId].wrong);
            }

            console.log('Local stats after answer:', playerStatsRef.current[myPlayerId]);

            const TIME_LIMIT_SECONDS = 30;
            const clampedTime = Math.max(0, Math.min(TIME_LIMIT_SECONDS, timeLeft ?? 0));
            const questionId = currentQuestion.id || allQuestions[gameState.currentQuestionIndex]?.id;

            // IMMEDIATELY update UI - don't wait for server
            setHasAnswered(true);
            setPlayerAnswers(prev => {
                const updated = {
                    ...prev,
                    [myPlayerId]: true
                };
                console.log('Immediately updated playerAnswers:', updated);
                return updated;
            });

            // Submit to REST API (backend will broadcast via SignalR)
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

            console.log('Answer submitted successfully to REST API');

        } catch (error) {
            console.error('Answer submission failed:', error);
            setHasAnswered(false);
            const myPlayerId = parseInt(user.playerId);
            setPlayerAnswers(prev => {
                const updated = { ...prev };
                delete updated[myPlayerId];
                return updated;
            });
        }
    };

    const handleNextQuestion = async () => {
        if (!isHost) return;

        try {
            console.log('Host advancing to next question');
            await fetch(`http://localhost:5216/games/${roomCode}/next`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });
        } catch (err) {
            console.error('Next question failed:', err);
        }
    };

    const handleTimerExpire = useCallback(async () => {
        if (timerExpireHandledRef.current) {
            console.log('Timer expire already handled for this question');
            return;
        }

        if (!isHost || !currentQuestion) {
            console.log('Not host or no current question, skipping timer expire');
            return;
        }

        timerExpireHandledRef.current = true;
        console.log('Timer expired - host handling');

        if (!hasAnswered) {
            const myPlayerId = parseInt(user.playerId);
            if (!playerStatsRef.current[myPlayerId]) {
                playerStatsRef.current[myPlayerId] = { correct: 0, wrong: 0 };
            }
            playerStatsRef.current[myPlayerId].wrong++;
            setWrongAnswerCount(playerStatsRef.current[myPlayerId].wrong);
            console.log('Host did not answer in time, marked as wrong. Total wrong:', playerStatsRef.current[myPlayerId].wrong);

            const questionId = currentQuestion.id || allQuestions[gameState.currentQuestionIndex]?.id;
            try {
                await fetch(`http://localhost:5216/games/${roomCode}/answer`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify({
                        questionId,
                        selectedAnswerIndexes: [],
                        timeLeft: 0
                    })
                });
            } catch (e) {
                console.error('Failed to submit empty answer:', e);
            }
        }

        await handleNextQuestion();
    }, [isHost, currentQuestion, hasAnswered, user.playerId, roomCode, allQuestions, gameState.currentQuestionIndex]);

    const handleBackToLobby = () => {
        onBack();
    };

    const answeredPlayersCount = Object.keys(playerAnswers).filter(
        id => playerAnswers[id] === true
    ).length;
    const totalPlayersCount = players.length;
    const currentQuestionIndex = gameState.currentQuestionIndex || 0;
    const totalQuestions = allQuestions.length;
    const isLastQuestion = currentQuestionIndex >= totalQuestions - 1;
    const currentPlayer = players.find(p => p.playerId === parseInt(user.playerId));
    const currentScore = currentPlayer?.score ?? currentPlayer?.currentScore ?? 0;

    console.log('Rendering - PlayerAnswers:', playerAnswers, 'Count:', answeredPlayersCount, '/', totalPlayersCount);

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
                            const normalizedPlayerId = parseInt(player.playerId ?? player.id, 10);
                            const hasAnswered = playerAnswers[normalizedPlayerId] === true;

                            return (
                                <div key={normalizedPlayerId} className="player-status">
                                    <div className="player-avatar-small">
                                        {player.name?.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="player-info">
                                        <div className="player-name-row">
                                            <span className="player-name">{player.name}</span>
                                            {normalizedPlayerId === parseInt(user.playerId) && (
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
                        onTimerExpire={handleTimerExpire}
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
