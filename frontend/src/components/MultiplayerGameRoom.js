import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGameRoom.css';
import signalRService from '../services/signalRService';

const API_BASE = '/api';

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

    // Stable load function using useRef to avoid stale closures
    const loadGameStateRef = useRef();
    loadGameStateRef.current = async () => {
        try {
            console.log('Loading game state for room:', roomCode);
            const response = await fetch(`${API_BASE}/games/${roomCode}`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (response.ok) {
                const gameDetails = await response.json();
                console.log('Game state loaded:', gameDetails);
                console.log('Players in response:', gameDetails.players?.length || 0);

                if (mounted.current) {
                    const hostId = gameDetails.hostPlayerId ?? gameDetails.HostPlayerId;
                    setIsHost(Number(hostId) === Number(user.playerId));

                    // Force update players state
                    const playersList = gameDetails.players || [];
                    console.log('Setting players state to:', playersList);
                    setPlayers(playersList);

                    const normalizedState = gameDetails.state?.toLowerCase() || 'waiting';
                    setGameState(normalizedState);

                    if (gameDetails.settings && !selectedCategory) {
                        setSelectedCategory(gameDetails.settings.category);
                    }
                }
            } else {
                console.error('Failed to load game state:', response.status);
            }
        } catch (error) {
            console.error('Failed to load game details:', error);
        }
    };

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

                // Ensure connection is established
                if (!signalRService.isConnected) {
                    console.log('Establishing SignalR connection...');
                    await signalRService.startConnection(token);
                    console.log('SignalR connection established');
                } else {
                    console.log('SignalR already connected');
                }

                setConnectionStatus('connected');

                // Make sure connection object is available
                if (!signalRService.connection) {
                    throw new Error('SignalR connection object not available');
                }

                console.log('SignalR connection state:', signalRService.connection.state);

                // Register event handlers BEFORE joining the room
                if (!listenersRegistered.current) {
                    console.log('Registering SignalR event handlers...');

                    // Player joined handler - use camelCase (SignalR converts PascalCase to camelCase)
                    signalRService.connection.on('PlayerJoined', async (data) => {
                        console.log('playerJoined event received:', data);
                        if (!mounted.current) return;
                        await loadGameStateRef.current();
                    });

                    // Player left handler - use camelCase
                    signalRService.connection.on('PlayerLeft', async (data) => {
                        console.log('playerLeft event received:', data);
                        if (!mounted.current) return;
                        await loadGameStateRef.current();
                    });

                    // Game started handler - use camelCase
                    signalRService.connection.on('GameStarted', (gameDetails) => {
                        if (!mounted.current) return;
                        console.log('gameStarted event received');

                        setGameState('in-progress');
                        setLoading(false);

                        let category = selectedCategory;

                        if (gameDetails?.settings?.category) {
                            category = gameDetails.settings.category;
                        } else if (gameDetails?.questions?.[0]?.category) {
                            category = gameDetails.questions[0].category;
                        }

                        onStartGame(category, roomCode);
                    });

                    // Game start failed handler - use camelCase
                    signalRService.connection.on('GameStartFailed', (errorMessage) => {
                        if (!mounted.current) return;
                        console.error('gameStartFailed:', errorMessage);
                        setError(`Failed to start game: ${errorMessage}`);
                        setLoading(false);
                    });

                    // Voting handlers - use camelCase
                    signalRService.connection.on('CategoryVotingTimer', (data) => {
                        if (!mounted.current) return;
                        const remaining = data.remainingSeconds ?? data.RemainingSeconds ?? null;
                        setRemainingSeconds(remaining);
                    });

                    signalRService.connection.on('CategoryRevoteStarted', (data) => {
                        console.log('categoryRevoteStarted', data);
                        if (!mounted.current) return;
                        setIsVoting(true);
                        setVotingCategories(data.categories || data.Categories || []);
                        setVoteTallies({});
                        setMyVote(null);
                        setRemainingSeconds(data.durationSeconds ?? data.DurationSeconds ?? null);
                        setIsCategoryLocked(false);
                    });

                    signalRService.connection.on('CategoryVotingStarted', (data) => {
                        console.log('categoryVotingStarted', data);
                        if (!mounted.current) return;
                        setIsVoting(true);
                        setVotingCategories(data.categories || data.Categories || []);
                        setVoteTallies({});
                        setMyVote(null);
                        setIsCategoryLocked(false);
                        const initial = data.durationSeconds ?? data.DurationSeconds ?? null;
                        setRemainingSeconds(initial);
                    });

                    signalRService.connection.on('CategoryVoteUpdated', (data) => {
                        console.log('categoryVoteUpdated', data);
                        if (!mounted.current) return;
                        const tallies = data.tallies || data.Tallies || {};
                        setVoteTallies(tallies);
                        const eventPlayerId = data.playerId ?? data.PlayerId;
                        const selectedCategory = data.selectedCategory ?? data.SelectedCategory;
                        if (parseInt(eventPlayerId) === parseInt(user.playerId)) {
                            setMyVote(selectedCategory);
                        }
                    });

                    signalRService.connection.on('CategoryVotingFinished', (data) => {
                        console.log('categoryVotingFinished', data);
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
                    });

                    // Game state sync handler - use camelCase
                    signalRService.connection.on('GameStateSync', (gameDetails) => {
                        if (!mounted.current) return;
                        console.log('gameStateSync received:', gameDetails);

                        const hostId = gameDetails.hostPlayerId ?? gameDetails.HostPlayerId;
                        setIsHost(Number(hostId) === Number(user.playerId));

                        const playersList = gameDetails.players || [];
                        console.log('gameStateSync: Setting players to:', playersList);
                        setPlayers(playersList);
                        setGameState(gameDetails.state?.toLowerCase() || 'waiting');
                    });

                    listenersRegistered.current = true;
                    console.log('All SignalR event handlers registered');

                    // Verify handlers were registered
                    console.log('Verifying handlers registered on connection...');
                }

                // Small delay to ensure handlers are ready
                await new Promise(resolve => setTimeout(resolve, 100));

                // NOW join the room - this ensures we receive all events
                console.log('Joining SignalR room:', roomCode);
                await signalRService.joinGameRoom(roomCode);
                console.log('Successfully joined SignalR room');

                // Wait a bit for GameStateSync event
                await new Promise(resolve => setTimeout(resolve, 500));

                // Load initial game state as fallback
                console.log('Loading initial game state...');
                await loadGameStateRef.current();

            } catch (error) {
                console.error('Failed to initialize SignalR connection:', error);
                setConnectionStatus('error');
                setError('Failed to connect to game room. Please try again.');
                // Still try to load game state even if SignalR fails
                await loadGameStateRef.current();
            }
        };

        initializeConnection();

        return () => {
            // Mark as unmounted
            mounted.current = false;

            // Clean up listeners on unmount
            if (listenersRegistered.current && signalRService.connection) {
                console.log('Cleaning up SignalR listeners');
                signalRService.connection.off('GameStateSync');
                signalRService.connection.off('PlayerJoined');
                signalRService.connection.off('PlayerLeft');
                signalRService.connection.off('GameStarted');
                signalRService.connection.off('GameStartFailed');
                signalRService.connection.off('CategoryVotingStarted');
                signalRService.connection.off('CategoryVoteUpdated');
                signalRService.connection.off('CategoryVotingFinished');
                signalRService.connection.off('CategoryRevoteStarted');
                signalRService.connection.off('CategoryVotingTimer');

                signalRService.leaveGameRoom(roomCode).catch(() => { });
                listenersRegistered.current = false;
            }

            // Reset initialization flag so it can run again if component remounts
            initializationDone.current = false;
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
                    }
                }
            } catch (error) {
                setError('Failed to load categories');
            } finally {
                setLoadingCategories(false);
            }
        };

        if (categories.length === 0) {
            loadCategories();
        }
    }, []);

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

            await new Promise(resolve => setTimeout(resolve, 500));

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

    console.log('Render - Players count:', players.length, 'Players:', players);

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
                            players.map((player, index) => {
                                const isCurrentUser = player.playerId === parseInt(user.playerId);
                                const isPlayerHost = isHost && isCurrentUser;

                                return (
                                    <div key={player.playerId || player.id || index} className="player-item">
                                        <div className="player-avatar">
                                            {(player.name?.charAt(0) || 'P').toUpperCase()}
                                        </div>
                                        <div className="player-info">
                                            <span className="player-name">{player.name}</span>
                                            <div className="player-badges">
                                                {isCurrentUser && (
                                                    <span className="you-badge">You</span>
                                                )}
                                                {isPlayerHost && (
                                                    <span className="host-badge">Host</span>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                );
                            }))}
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
