import React, { useState, useEffect } from 'react';
import './MultiplayerLobby.css';
import signalRService from '../services/signalRService';

const MultiplayerLobby = ({ onBack, onCreateGame, onJoinGame, user }) => {
    const [roomCode, setRoomCode] = useState('');
    const [isCreating, setIsCreating] = useState(false);
    const [isJoining, setIsJoining] = useState(false);
    const [error, setError] = useState('');
    const [connectionReady, setConnectionReady] = useState(false);

    // Pre-establish SignalR connection when lobby mounts
    useEffect(() => {
        const initConnection = async () => {
            try {
                const token = localStorage.getItem('token');
                if (token) {
                    console.log('MultiplayerLobby: Establishing SignalR connection...');
                    await signalRService.startConnection(token);
                    console.log('MultiplayerLobby: SignalR connection ready');
                    setConnectionReady(true);
                }
            } catch (error) {
                console.error('Failed to establish SignalR connection:', error);
                setError('Failed to connect. Please refresh and try again.');
            }
        };

        initConnection();

        // Cleanup - but don't stop connection, just mark as not ready
        return () => {
            setConnectionReady(false);
        };
    }, []);

    const handleCreateGame = async () => {
        setIsCreating(true);
        setError('');

        try {
            // Ensure connection is ready
            if (!connectionReady) {
                console.log('Connection not ready, waiting...');
                await signalRService.waitForConnection(5000);
                setConnectionReady(true);
            }

            // Generate a random room code
            const newRoomCode = Math.random().toString(36).substring(2, 8).toUpperCase();

            console.log('Creating game room:', newRoomCode);

            const response = await fetch(`http://localhost:5216/games/${newRoomCode}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!response.ok) {
                throw new Error('Failed to create game');
            }

            console.log('Game created successfully, navigating to room...');

            // Small delay to ensure backend is ready
            await new Promise(resolve => setTimeout(resolve, 300));

            // Navigate to game room - connection is already established
            onCreateGame(newRoomCode);
        } catch (err) {
            console.error('Error creating game:', err);
            setError(err.message);
        } finally {
            setIsCreating(false);
        }
    };

    const handleJoinGame = async () => {
        if (!roomCode.trim()) {
            setError('Please enter a room code');
            return;
        }

        setIsJoining(true);
        setError('');

        try {
            // Ensure connection is ready
            if (!connectionReady) {
                console.log('Connection not ready, waiting...');
                await signalRService.waitForConnection(5000);
                setConnectionReady(true);
            }

            console.log('Joining game room:', roomCode.toUpperCase());

            const response = await fetch(`http://localhost:5216/games/${roomCode.toUpperCase()}/join`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Failed to join game');
            }

            console.log('Joined game successfully, navigating to room...');

            // Small delay to ensure backend is ready
            await new Promise(resolve => setTimeout(resolve, 300));

            // Navigate to game room - connection is already established
            onJoinGame(roomCode.toUpperCase());
        } catch (err) {
            console.error('Error joining game:', err);
            setError(err.message);
        } finally {
            setIsJoining(false);
        }
    };

    return (
        <div className="lobby-container">
            <div className="lobby-card">
                <h2 className="lobby-title">Multiplayer Lobby</h2>
                <p className="lobby-subtitle">Create or join a game room</p>

                {error && (
                    <div className="error-message">
                        {error}
                    </div>
                )}

                <div className="lobby-actions">
                    <div className="create-game-section">
                        <h3>Create New Game</h3>
                        <p>Start a new game and invite friends</p>
                        <button
                            className="create-button"
                            onClick={handleCreateGame}
                            disabled={isCreating || isJoining || !connectionReady}
                        >
                            {isCreating ? (
                                <>
                                    <div className="loading-spinner-small"></div>
                                    <span>Creating...</span>
                                </>
                            ) : 'Create Game'}
                        </button>
                    </div>

                    <div className="divider">
                        <span>OR</span>
                    </div>

                    <div className="join-game-section">
                        <h3>Join Game</h3>
                        <p>Enter a room code to join an existing game</p>
                        <div className="join-form">
                            <input
                                type="text"
                                placeholder="Enter room code"
                                value={roomCode}
                                onChange={(e) => setRoomCode(e.target.value.toUpperCase())}
                                maxLength={6}
                                className="room-code-input"
                                disabled={isCreating || isJoining || !connectionReady}
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter' && roomCode.trim() && connectionReady) {
                                        handleJoinGame();
                                    }
                                }}
                            />
                            <button
                                className="join-button"
                                onClick={handleJoinGame}
                                disabled={isCreating || isJoining || !roomCode.trim() || !connectionReady}
                            >
                                {isJoining ? (
                                    <>
                                        <div className="loading-spinner-small"></div>
                                        <span>Joining...</span>
                                    </>
                                ) : 'Join Game'}
                            </button>
                        </div>
                    </div>
                </div>

                <button className="back-button" onClick={onBack} disabled={isCreating || isJoining}>
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                        <path d="M8 0L0 8l8 8V0z" />
                    </svg>
                    Back to Game Modes
                </button>
            </div>
        </div>
    );
};

export default MultiplayerLobby;
