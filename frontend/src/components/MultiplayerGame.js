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

    const correctAnswerCountRef = useRef(0);
    const wrongAnswerCountRef = useRef(0);
    const statsUpdatedRef = useRef(false);
    const listenersRegistered = useRef(false);
    const mounted = useRef(true);

    useEffect(() => {
        mounted.current = true;
        return () => { mounted.current = false; };
    }, []);

    const handleGameFinished = (finalData) => {
        if (!mounted.current) return;

        if (statsUpdatedRef.current) {
            console.log('Stats already updated, skipping duplicate update');
            return;
        }

        console.log('Game finished handler called with data:', finalData);

        const rawPlayers = finalData.leaderboard || finalData.players || [];

        const leaderboard = rawPlayers.map(player => ({
            playerId: player.playerId ?? player.id,
            name: player.name,
            score: player.score ?? player.currentScore ?? 0,
            correct: correctAnswerCountRef.current,
            wrong: wrongAnswerCountRef.current,
        })).sort((a, b) => b.score - a.score);

        const totalQuestions =
            (correctAnswerCountRef.current + wrongAnswerCountRef.current) ||
            finalData.totalQuestions ||
            allQuestions.length;

        const processedResults = {
            players: leaderboard,
            leaderboard,
            correctCount: correctAnswerCountRef.current,
            wrongCount: wrongAnswerCountRef.current,
            totalQuestions
        };

        console.log('Processed results for MultiplayerResults:', processedResults);

        statsUpdatedRef.current = true;
        updateMultiplayerStatistics(processedResults, leaderboard);

        if (onGameFinished) {
            onGameFinished(processedResults);
        }

        setFinalResults(processedResults);
        setGameFinished(true);
    };

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

        const interval = setInterval(checkGameCompletion, 1000);
        return () => clearInterval(interval);
    }, [roomCode, gameFinished, user.playerId]);

    useEffect(() => {
        let nextQuestionHandler, answerSubmittedHandler, gameFinishedHandler;

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

                gameFinishedHandler = (finalData) => {
                    console.log('GameFinished SignalR event received by player:', user.playerId, finalData);
                    if (!mounted.current) return;
                    handleGameFinished(finalData);
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

                        if (gameDetails.state === 'Finished') {
                            console.log('Game already finished when loading state');
                            handleGameFinished(gameDetails);
                            return;
                        }

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

    const updateMultiplayerStatistics = async (finalData, leaderboard) => {
        try {
            const totalQuestions = finalData.totalQuestions ?? allQuestions.length;
            const correctAnswers = finalData.correctCount ?? 0;

            const currentPlayerData = leaderboard.find(p => p.playerId === parseInt(user.playerId));
            const score = currentPlayerData?.score ?? 0;

            const category =
                gameState.settings?.category ||
                allQuestions[0]?.category ||
                'General';

            console.log('Updating multiplayer stats:', {
                category,
                score,
                correctAnswers,
                totalQuestions
            });

            await fetch('http://localhost:5216/statistics/update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    category,
                    score,
                    correctAnswers,
                    totalQuestions
                })
            });

            console.log('Multiplayer statistics updated successfully');
        } catch (error) {
            console.error('Failed to update multiplayer statistics:', error);
        }
    };

    const handleAnswerSelect = async (selectedIndex, timeLeft) => {
        if (!currentQuestion || hasAnswered) return;

        try {
            setHasAnswered(true);

            const selectedIndexes = Array.isArray(selectedIndex) ? selectedIndex : [selectedIndex];
            const correctAnswer = currentQuestion.correctAnswerIndexes || [];

            const sortedSelected = [...selectedIndexes].sort((a, b) => a - b);
            const sortedCorrect = [...correctAnswer].sort((a, b) => a - b);

            const isCorrect = JSON.stringify(sortedSelected) === JSON.stringify(sortedCorrect);

            if (isCorrect) {
                correctAnswerCountRef.current += 1;
                setCorrectAnswerCount(correctAnswerCountRef.current);
            } else {
                wrongAnswerCountRef.current += 1;
                setWrongAnswerCount(wrongAnswerCountRef.current);
            }

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
        setGameFinished(false);
        setFinalResults(null);
        setCurrentQuestion(null);
        setHasAnswered(false);
        setPlayerAnswers({});
        setCorrectAnswerCount(0);
        setWrongAnswerCount(0);
        correctAnswerCountRef.current = 0;
        wrongAnswerCountRef.current = 0;
        statsUpdatedRef.current = false;
        console.log('Play again requested');
    };

    const handleBackToLobby = () => {
        onBack();
    };

    const answeredPlayersCount = Object.values(playerAnswers).filter(v => v === true).length;
    const totalPlayersCount = players.length;
    const currentQuestionIndex = gameState.currentQuestionIndex || 0;
    const totalQuestions = allQuestions.length;
    const isLastQuestion = currentQuestionIndex >= totalQuestions - 1;

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
