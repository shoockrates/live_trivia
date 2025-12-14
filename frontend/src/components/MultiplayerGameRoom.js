import React, { useState, useEffect, useRef, useCallback } from 'react';
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

    const [isCategoryLocked, setIsCategoryLocked] = useState(false);
    const [isVoting, setIsVoting] = useState(false);
    const [votingCategories, setVotingCategories] = useState([]);
    const [voteTallies, setVoteTallies] = useState({});
    const [myVote, setMyVote] = useState(null);
    const [remainingSeconds, setRemainingSeconds] = useState(null);

    const listenersRegistered = useRef(false);
    const mounted = useRef(true);
    const initializationDone = useRef(false);

    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);

    // Stable load function that doesn't cause re-renders
    const loadGameState = useCallback(async () => {
        try {
            const response = await fetch(`${API_BASE}/games/${roomCode}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (response.ok) {
                const gameDetails = await response.json();
                console.log('Game state loaded:', gameDetails);

                if (mounted.current) {
                    const hostId = gameDetails.hostPlayerId ?? gameDetails.HostPlayerId;
                    setIsHost(Number(hostId) === Number(user.playerId));

                    setPlayers(gameDetails.players || []);

                    const normalizedState = gameDetails.state?.toLowerCase() || 'waiting';
                    setGameState(normalizedState);

                    if (gameDetails.settings && !selectedCategory) {
                        setSelectedCategory(gameDetails.settings.category);
                    }
                }
            }
        } catch (error) {
            console.error('Failed to load game details:', error);
        }
    }, [roomCode, user.playerId, selectedCategory]);

    useEffect(() => {
        // Prevent multiple initializations
        if (initializationDone.current) {
            return;
        }
        initializationDone.current = true;

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

                const playerJoinedHandler = async (data) => {
                    console.log('Player joined event received:', data);
                    if (!mounted.current) return;

                    if (data.GameState && data.GameState.players) {
                        setPlayers(data.GameState.players);
                        console.log('Updated players from PlayerJoined event:', data.GameState.players);
                    } else {
                        console.log('No GameState in PlayerJoined event, reloading...');
                        await loadGameState();
                    }
                };

                const playerLeftHandler = async (data) => {
                    console.log('Player left event received:', data);
                    if (!mounted.current) return;

                    if (data.GameState && data.GameState.players) {
                        setPlayers(data.GameState.players);
                        console.log('Updated players from PlayerLeft event:', data.GameState.players);
                    } else {
                        console.log('No GameState in PlayerLeft event, reloading...');
                        await loadGameState();
                    }
                };

                const gameStartedHandler = (gameDetails) => {
                    if (!mounted.current) return;
                    console.log('GameStarted event received with details:', gameDetails);

                    setGameState('in-progress');
                    setLoading(false);

                    // Try to extract category from various possible locations
                    let category = selectedCategory; // fallback to what we have

                    if (gameDetails?.settings?.category) {
                        category = gameDetails.settings.category;
                    } else if (gameDetails?.questions?.[0]?.category) {
                        category = gameDetails.questions[0].category;
                    }

                    console.log('Extracted category:', category);
                    console.log('Calling onStartGame with:', { category, roomCode });

                    // Navigate to the multiplayer game
                    onStartGame(category, roomCode);
                };

                const gameStartFailedHandler = (errorMessage) => {
                    if (!mounted.current) return;
                    console.error('Game start failed:', errorMessage);
                    setError(`Failed to start game: ${errorMessage}`);
                    setLoading(false);
                };

                const votingTimerHandler = (data) => {
                    if (!mounted.current) return;
                    const remaining = data.remainingSeconds ?? data.RemainingSeconds ?? null;
                    setRemainingSeconds(remaining);
                };

                const revoteStartedHandler = (data) => {
                    console.log('CategoryRevoteStarted', data);
                    if (!mounted.current) return;
                    setIsVoting(true);
                    setVotingCategories(data.categories || data.Categories || []);
                    setVoteTallies({});
                    setMyVote(null);
                    setRemainingSeconds(data.durationSeconds ?? data.DurationSeconds ?? null);
                    setIsCategoryLocked(false);
                };

                const votingStartedHandler = (data) => {
                    console.log('CategoryVotingStarted', data);
                    if (!mounted.current) return;
                    setIsVoting(true);
                    setVotingCategories(data.categories || data.Categories || []);
                    setVoteTallies({});
                    setMyVote(null);
                    setIsCategoryLocked(false);
                    const initial = data.durationSeconds ?? data.DurationSeconds ?? null;
                    setRemainingSeconds(initial);
                };

                const voteUpdatedHandler = (data) => {
                    console.log('CategoryVoteUpdated', data);
                    if (!mounted.current) return;
                    const tallies = data.tallies || data.Tallies || {};
                    setVoteTallies(tallies);
                    const eventPlayerId = data.playerId ?? data.PlayerId;
                    const selectedCategory = data.selectedCategory ?? data.SelectedCategory;
                    if (parseInt(eventPlayerId) === parseInt(user.playerId)) {
                        setMyVote(selectedCategory);
                    }
                };

                const votingFinishedHandler = (data) => {
                    console.log('CategoryVotingFinished', data);
                    if (!mounted.current) return;
                    const winning = data.winningCategory || data.WinningCategory || null;
                    const isFinal = data.isFinal ?? data.IsFinal ?? false;
                    setIsVoting(false);
                    setVotingCategories([]);
                    setVoteTallies({});
                    setMyVote(null);
                    setRemainingSeconds(null);
                    if (winning) {
                        setSelectedCategory(winning);
                    }
                    if (isFinal && winning) {
                        setIsCategoryLocked(true);
                    }
                };

                const gameStateSyncHandler = (gameDetails) => {
                    if (!mounted.current) return;
                    console.log('GameStateSync received:', gameDetails);

                    const hostId = gameDetails.hostPlayerId ?? gameDetails.HostPlayerId;
                    setIsHost(Number(hostId) === Number(user.playerId));

                    setPlayers(gameDetails.players || []);
                    setGameState(gameDetails.state?.toLowerCase() || 'waiting');
                };

                // Register all listeners
                if (!listenersRegistered.current) {
                    signalRService.onGameStateSync(gameStateSyncHandler);
                    signalRService.onPlayerJoined(playerJoinedHandler);
                    signalRService.onPlayerLeft(playerLeftHandler);
                    signalRService.onGameStarted(gameStartedHandler);
                    signalRService.onGameStartFailed(gameStartFailedHandler);
                    signalRService.onCategoryRevoteStarted(revoteStartedHandler);
                    signalRService.onCategoryVotingTimer(votingTimerHandler);
                    signalRService.onCategoryVotingStarted(votingStartedHandler);
                    signalRService.onCategoryVoteUpdated(voteUpdatedHandler);
                    signalRService.onCategoryVotingFinished(votingFinishedHandler);

                    listenersRegistered.current = true;
                }

                // Wait a bit for connection to stabilize
                await new Promise(resolve => setTimeout(resolve, 500));

                // Join the room
                await signalRService.joinGameRoom(roomCode);

                // Load initial game state
                await loadGameState();

            } catch (error) {
                console.error('Failed to initialize SignalR connection:', error);
                setConnectionStatus('error');
                setError('Failed to connect to game room. Please try again.');
                // Still try to load game state via HTTP
                await loadGameState();
            }
        };

        initializeConnection();

        return () => {
            // Only clean up when component actually unmounts
            if (!mounted.current && listenersRegistered.current) {
                signalRService.removeListener('GameStateSync');
                signalRService.removeListener('PlayerJoined');
                signalRService.removeListener('PlayerLeft');
                signalRService.removeListener('GameStarted');
                signalRService.removeListener('GameStartFailed');
                signalRService.removeListener('CategoryVotingStarted');
                signalRService.removeListener('CategoryVoteUpdated');
                signalRService.removeListener('CategoryVotingFinished');
                signalRService.removeListener('CategoryRevoteStarted');
                signalRService.removeListener('CategoryVotingTimer');

                signalRService.leaveGameRoom(roomCode).catch(() => { });
                listenersRegistered.current = false;
            }
        };
    }, []); // Empty array - only run once

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
                        // Only set default category if none is selected and user is host
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

        // Only load categories once
        if (categories.length === 0) {
            loadCategories();
        }
    }, [isHost, selectedCategory]); // Stable dependencies

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
            console.log('Starting game with category:', selectedCategory);

            // Update game settings
            const settingsResponse = await fetch(`${API_BASE}/games/${roomCode}/settings`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    category: selectedCategory,
                    questionCount: 5,
                    timeLimitSeconds: 30,
                    difficulty: "any"
                })
            });

            if (!settingsResponse.ok) {
                const errorText = await settingsResponse.text();
                throw new Error(`Failed to update game settings: ${errorText}`);
            }

            // Wait for settings to persist
            await new Promise(resolve => setTimeout(resolve, 500));

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

    const handleStartVoting = async () => {
        try {
            const options = categories.length > 0 ? categories : [];
            if (options.length === 0) {
                setError('No categories available to vote on');
                return;
            }
            await signalRService.startCategoryVoting(roomCode, options);
            setError('');
        } catch (err) {
            console.error('Failed to start voting:', err);
            setError('Failed to start category voting');
        }
    };

    const handleVote = async (category) => {
        try {
            await signalRService.submitCategoryVote(roomCode, category);
            setError('');
        } catch (err) {
            console.error('Failed to submit vote:', err);
            setError('Failed to submit vote');
        }
    };

    const handleEndVoting = async () => {
        try {
            await signalRService.endCategoryVoting(roomCode);
            setError('');
        } catch (err) {
            console.error('Failed to end voting:', err);
            setError('Failed to end voting');
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

    const isWaitingState = gameState === 'waiting' || gameState === 'waitingforplayers';
    const shouldShowGameSetup = isWaitingState && isHost;

    return (
        <div className="multiplayer-game-room-container">
            <div className="multiplayer-game-room-card">
                <div className="room-header">
                    <h2 className="room-title">Game Room</h2>
                    <div className="room-code-section">
                        <span className="room-code-label">Room Code</span>
                        <div className="room-code-display">
                            <span className="room-code">{roomCode}</span>
                            <button className="copy-button" onClick={copyRoomCode} title="Copy room code">
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

                {error && <div className="error-message">{error}</div>}

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

                        {!isVoting && (
                            <>
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
                                                    onClick={() => {
                                                        if (isCategoryLocked) return;
                                                        setSelectedCategory(category);
                                                    }}
                                                    disabled={isCategoryLocked}
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

                                {isHost && (
                                    <>
                                        <div className="or-divider"><span>OR</span></div>
                                        <div className="voting-start">
                                            <label>Let players vote for category</label>
                                            <button
                                                className="start-voting-button"
                                                onClick={handleStartVoting}
                                                disabled={players.length < 2 || categories.length === 0}
                                            >
                                                Start Category Voting
                                            </button>
                                            {players.length < 2 && (
                                                <div className="info-message">
                                                    <span>Need at least 2 players to vote.</span>
                                                </div>
                                            )}
                                        </div>
                                    </>
                                )}
                            </>
                        )}

                        <button
                            className="start-game-button"
                            onClick={handleStartGame}
                            disabled={!selectedCategory || loading || players.length < 2 || isVoting}
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

                {isWaitingState && isVoting && (
                    <div className="voting-panel">
                        <h4>Category Voting in Progress</h4>
                        <p>Tap a category to vote:</p>

                        {typeof remainingSeconds === 'number' && (
                            <div className="voting-timeout">
                                <svg viewBox="0 0 16 16">
                                    <circle cx="8" cy="8" r="7" stroke="currentColor" fill="none" />
                                    <path d="M8 3v5l3 2" stroke="currentColor" fill="none" />
                                </svg>
                                <span>Voting ends in {remainingSeconds}s</span>
                            </div>
                        )}

                        <div className="voting-options">
                            {votingCategories.map((cat) => (
                                <button
                                    key={cat}
                                    className={`vote-option ${myVote === cat ? 'selected' : ''}`}
                                    onClick={() => handleVote(cat)}
                                >
                                    <span>{cat}</span>
                                    <small>
                                        {(voteTallies[cat] || 0)} vote{(voteTallies[cat] || 0) !== 1 ? 's' : ''}
                                    </small>
                                </button>
                            ))}
                        </div>
                        {isHost && (
                            <button className="finish-voting-button" onClick={handleEndVoting}>
                                Finish Voting & Use Winner
                            </button>
                        )}
                    </div>
                )}

                {isWaitingState && !isHost && !isVoting && (
                    <div className="waiting-message">
                        <div className="waiting-spinner"></div>
                        <p>Waiting for host to start the game...</p>
                        <p className="players-count">Players in room: {players.length}</p>
                        {selectedCategory && (
                            <p className="category-info">Category: {selectedCategory}</p>
                        )}
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
