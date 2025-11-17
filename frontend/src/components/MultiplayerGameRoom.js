import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGameRoom.css';
import signalRService from '../services/signalRService';

const API_BASE = 'http://localhost:5216';

const MultiplayerGameRoom = ({ roomCode, user, onBack, onStartGame }) => {
    const [players, setPlayers] = useState([]);
    const [gameState, setGameState] = useState('waiting');
    const [isHost, setIsHost] = useState(false);
    const [selectedCategory, setSelectedCategory] = useState(null);
    const [categories, setCategories] = useState([]);
    const [loading, setLoading] = useState(false);
    const [loadingCategories, setLoadingCategories] = useState(true);
    const [error, setError] = useState('');
    const [connectionStatus, setConnectionStatus] = useState('disconnected');

    const listenersRegistered = useRef(false);
    const mounted = useRef(true);
    const connectionInitialized = useRef(false);

    useEffect(() => {
        mounted.current = true;

        return () => {
            mounted.current = false;
            connectionInitialized.current = false;
        };
    }, []);

    useEffect(() => {
        if (connectionInitialized.current) {
            return;
        }
        connectionInitialized.current = true;

        let playerJoinedHandler;
        let playerLeftHandler;
        let gameStartedHandler;
        let gameStartFailedHandler;

        const initializeConnection = async () => {
            try {
                const token = localStorage.getItem('token');
                if (!token) {
                    setError('No authentication token found');
                    return;
                }

                setConnectionStatus('connecting');

                await signalRService.startConnection(token);
                setConnectionStatus('connected');

                playerJoinedHandler = (data) => {
                    console.log('Player joined event received - FULL DATA:', data);
                    if (!mounted.current) return;

                    // Force update the game state from the server
                    loadGameState().catch(error => {
                        console.error('Failed to reload game state after player joined:', error);
                    });
                };


                playerLeftHandler = (data) => {
                    console.log('Player left event received:', data);
                    if (!mounted.current) return;

                    // Force update the game state from the server
                    loadGameState().catch(error => {
                        console.error('Failed to reload game state after player left:', error);
                    });
                };

                gameStartedHandler = (gameDetails) => {
                    if (!mounted.current) return;

                    setGameState('in-progress');
                    onStartGame(selectedCategory, roomCode);
                };

                gameStartFailedHandler = (errorMessage) => {
                    if (!mounted.current) return;

                    setError(`Failed to start game: ${errorMessage}`);
                    setLoading(false);
                };

                signalRService.removeAllListeners('PlayerJoined');
                signalRService.removeAllListeners('PlayerLeft');
                signalRService.removeAllListeners('GameStarted');
                signalRService.removeAllListeners('GameStartFailed');

                signalRService.onPlayerJoined(playerJoinedHandler);
                signalRService.onPlayerLeft(playerLeftHandler);
                signalRService.onGameStarted(gameStartedHandler);
                signalRService.onGameStartFailed(gameStartFailedHandler);

                listenersRegistered.current = true;

                await new Promise(resolve => setTimeout(resolve, 500));

                await signalRService.joinGameRoom(roomCode);
                await loadGameState();

            } catch (error) {
                console.error('Failed to initialize SignalR connection:', error);
                setConnectionStatus('error');
                setError('Failed to connect to game room. Please try again.');
                await loadGameState();
            }
        };

        const loadGameState = async () => {
            try {
                const response = await fetch(`${API_BASE}/games/${roomCode}`, {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (response.ok) {
                    const gameDetails = await response.json();

                    if (mounted.current) {
                        setIsHost(gameDetails.hostPlayerId === parseInt(user.playerId));
                        setPlayers(gameDetails.players || []);

                        const normalizedState = gameDetails.state?.toLowerCase() || 'waiting';
                        setGameState(normalizedState);

                        if (gameDetails.settings) {
                            setSelectedCategory(gameDetails.settings.category);
                        } else {
                            await loadGameSettings();
                        }
                    }
                }
            } catch (error) {
                console.error('Failed to load game details:', error);
            }
        };

        const loadGameSettings = async () => {
            try {
                const response = await fetch(`${API_BASE}/games/${roomCode}/settings`, {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (response.ok) {
                    const settings = await response.json();
                    if (mounted.current && settings.category) {
                        setSelectedCategory(settings.category);
                    }
                }
            } catch (error) {
                console.error('Failed to load game settings:', error);
            }
        };

        initializeConnection();

        return () => {
            if (listenersRegistered.current) {
                if (playerJoinedHandler) {
                    signalRService.removeListener('PlayerJoined', playerJoinedHandler);
                }
                if (playerLeftHandler) {
                    signalRService.removeListener('PlayerLeft', playerLeftHandler);
                }
                if (gameStartedHandler) {
                    signalRService.removeListener('GameStarted', gameStartedHandler);
                }
                if (gameStartFailedHandler) {
                    signalRService.removeListener('GameStartFailed', gameStartFailedHandler);
                }
                listenersRegistered.current = false;
            }
            connectionInitialized.current = false;
        };
    }, [roomCode, user.playerId, onStartGame]);

    useEffect(() => {
        const loadCategories = async () => {
            try {
                setLoadingCategories(true);
                const response = await fetch(`${API_BASE}/questions`, {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (response.ok) {
                    const questions = await response.json();

                    const uniqueCategories = Array.from(
                        new Set(questions.map(q => q.category || q.Category || '').filter(Boolean))
                    ).sort();

                    if (mounted.current) {
                        setCategories(uniqueCategories);
                        if (!selectedCategory && uniqueCategories.length > 0 && isHost) {
                            setSelectedCategory(uniqueCategories[0]);
                        }
                    }
                } else {
                    setError('Failed to load categories');
                }
            } catch (error) {
                setError('Failed to load categories');
            } finally {
                setLoadingCategories(false);
            }
        };

        loadCategories();
    }, [isHost]);

    useEffect(() => {
        if (isHost && categories.length > 0 && !selectedCategory) {
            setSelectedCategory(categories[0]);
        }
    }, [isHost, categories, selectedCategory]);

    const handleStartGame = async () => {
        if (!selectedCategory) {
            setError('Please select a category first');
            return;
        }

        if (players.length < 2) {
            setError('Need at least 2 players to start the game');
            return;
        }

        setLoading(true);
        setError('');

        try {
            console.log('Starting game with category:', selectedCategory, 'Players:', players.length);

            // First, let's check ALL questions to see what categories exist
            const allQuestionsResponse = await fetch(`${API_BASE}/questions`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (allQuestionsResponse.ok) {
                const allQuestions = await allQuestionsResponse.json();
                console.log('ALL QUESTIONS:', allQuestions);

                // Count questions by category
                const categoryCounts = {};
                allQuestions.forEach(q => {
                    const category = q.category || q.Category;
                    categoryCounts[category] = (categoryCounts[category] || 0) + 1;
                });
                console.log('QUESTIONS BY CATEGORY:', categoryCounts);
            }

            // Now check questions for the selected category
            const questionsResponse = await fetch(`${API_BASE}/questions/category/${encodeURIComponent(selectedCategory)}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!questionsResponse.ok) {
                throw new Error(`Failed to load questions for category: ${selectedCategory}`);
            }

            const questions = await questionsResponse.json();
            console.log(`Found ${questions.length} questions for category: "${selectedCategory}"`);
            console.log('QUESTIONS FOUND:', questions);

            if (questions.length === 0) {
                throw new Error(`No questions available for category: ${selectedCategory}. Please select a different category.`);
            }

            // Use the available number of questions
            const questionCount = Math.min(5, questions.length);

            console.log(`Will request ${questionCount} questions (${questions.length} available)`);

            // Update game settings
            const settingsResponse = await fetch(`${API_BASE}/games/${roomCode}/settings`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    category: selectedCategory,
                    questionCount: questionCount,
                    timeLimitSeconds: 30,
                    difficulty: "any"
                })
            });

            if (!settingsResponse.ok) {
                const errorText = await settingsResponse.text();
                throw new Error(`Failed to update game settings: ${errorText}`);
            }

            console.log(`Game settings updated with ${questionCount} questions`);

            // Wait for settings to persist
            await new Promise(resolve => setTimeout(resolve, 500));

            const verifySettingsResponse = await fetch(`${API_BASE}/games/${roomCode}/settings`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (verifySettingsResponse.ok) {
                const currentSettings = await verifySettingsResponse.json();
                console.log('CURRENT SETTINGS AFTER UPDATE:', currentSettings);

                // Check if the category and difficulty are what we expect

                if (currentSettings.category.toLowerCase() !== selectedCategory.toLowerCase() || currentSettings.difficulty !== "any") {
                    throw new Error(`Settings not updated correctly. Expected: ${selectedCategory}, any. Got: ${currentSettings.category}, ${currentSettings.difficulty}`);
                }
            }

            // Start the game
            const startResponse = await fetch(`${API_BASE}/games/${roomCode}/start`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!startResponse.ok) {
                const errorText = await startResponse.text();
                throw new Error(errorText || 'Failed to start game');
            }

            console.log('Game started successfully!');

        } catch (error) {
            console.error('Failed to start game:', error);
            setError('Failed to start game: ' + error.message);
            setLoading(false);
        }
    };

    const copyRoomCode = () => {
        navigator.clipboard.writeText(roomCode);
        const btn = document.querySelector('.copy-button');
        if (btn) {
            const originalText = btn.innerHTML;
            btn.innerHTML = '✓ Copied!';
            setTimeout(() => {
                btn.innerHTML = originalText;
            }, 2000);
        }
    };

    const handleBack = () => {
        onBack();
    };

    const shouldShowGameSetup = (gameState === 'waiting' || gameState === 'waitingforplayers') && isHost;

    return (
        <div className="multiplayer-game-room-container">
            <div className="multiplayer-game-room-card">
                <div className="room-header">
                    <h2 className="room-title">Game Room</h2>
                    <div className="room-code-section">
                        <span className="room-code-label">Room Code</span>
                        <div className="room-code-display">
                            <span className="room-code">{roomCode}</span>
                            <button
                                className="copy-button"
                                onClick={copyRoomCode}
                                title="Copy room code"
                            >
                                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                                    <path d="M4 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V2zm2-1a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H6z" />
                                    <path d="M2 5a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-1h1v1a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h1v1H2z" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>

                <div className="connection-status">
                    <div className={`status-indicator ${connectionStatus}`}>
                        {connectionStatus === 'connected' ? '🟢 Connected' :
                            connectionStatus === 'connecting' ? '🟡 Connecting...' :
                                connectionStatus === 'error' ? '🔴 Connection Error' : '⚫ Disconnected'}
                    </div>
                </div>

                {error && (
                    <div className="error-message">
                        {error}
                    </div>
                )}

                <div className="players-section">
                    <h3>Players ({players.length})</h3>
                    <div className="players-list">
                        {players.length === 0 ? (
                            <div className="no-players-message">
                                <p>Waiting for players to join...</p>
                            </div>
                        ) : (
                            players.map((player, index) => (
                                <div key={player.playerId || player.id || index} className="player-item">
                                    <div className="player-avatar">
                                        {(player.name?.charAt(0) || 'P').toUpperCase()}
                                    </div>
                                    <div className="player-info">
                                        <span className="player-name">{player.name}</span>
                                        {player.playerId === parseInt(user.playerId) && (
                                            <span className="you-badge">You</span>
                                        )}
                                        {isHost && player.playerId === parseInt(user.playerId) && (
                                            <span className="host-badge">Host</span>
                                        )}
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </div>

                {shouldShowGameSetup && (
                    <div className="game-setup">
                        <h3>Game Settings</h3>
                        <div className="category-selection">
                            <label>Select Category</label>
                            {loadingCategories ? (
                                <div className="loading-categories">
                                    <div className="loading-spinner-small"></div>
                                    <span>Loading categories...</span>
                                </div>
                            ) : categories.length > 0 ? (
                                <div className="category-grid">
                                    {categories.map((category) => (
                                        <button
                                            key={category}
                                            className={`category-button ${selectedCategory === category ? 'selected' : ''}`}
                                            onClick={() => setSelectedCategory(category)}
                                        >
                                            {category}
                                        </button>
                                    ))}
                                </div>
                            ) : (
                                <div className="no-categories-message">
                                    <span>No categories available</span>
                                </div>
                            )}
                        </div>

                        <button
                            className="start-game-button"
                            onClick={handleStartGame}
                            disabled={!selectedCategory || loading || players.length < 2}
                        >
                            {loading ? (
                                <>
                                    <div className="loading-spinner-small"></div>
                                    Starting Game...
                                </>
                            ) : (
                                `Start Game (${players.length} player${players.length !== 1 ? 's' : ''})`
                            )}
                        </button>

                        {players.length < 2 && (
                            <div className="info-message">
                                <span>Share the room code with friends to play together!</span>
                            </div>
                        )}
                    </div>
                )}

                {gameState === 'waiting' && !isHost && (
                    <div className="waiting-message">
                        <div className="waiting-spinner"></div>
                        <p>Waiting for host to start the game...</p>
                        <p className="players-count">Players in room: {players.length}</p>
                        {selectedCategory && <p className="category-info">Category: {selectedCategory}</p>}
                    </div>
                )}

                {gameState === 'in-progress' && (
                    <div className="game-in-progress">
                        <div className="loading-spinner"></div>
                        <p>Game is starting...</p>
                        <p>Redirecting to game...</p>
                    </div>
                )}

                <button className="back-button" onClick={handleBack}>
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                        <path fillRule="evenodd" d="M15 8a.5.5 0 0 0-.5-.5H2.707l3.147-3.146a.5.5 0 1 0-.708-.708l-4 4a.5.5 0 0 0 0 .708l4 4a.5.5 0 0 0 .708-.708L2.707 8.5H14.5A.5.5 0 0 0 15 8z" />
                    </svg>
                    Leave Room
                </button>
            </div>
        </div>
    );
};

export default MultiplayerGameRoom;
