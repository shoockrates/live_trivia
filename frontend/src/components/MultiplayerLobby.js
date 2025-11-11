import React, { useState, useEffect } from 'react';
import './MultiplayerLobby.css';

const MultiplayerLobby = ({ onBack, onCreateGame, onJoinGame, user }) => {
  const [roomCode, setRoomCode] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const [error, setError] = useState('');

  const handleCreateGame = async () => {
    setIsCreating(true);
    setError('');
    
    try {
      // Generate a random room code
      const newRoomCode = Math.random().toString(36).substring(2, 8).toUpperCase();
      
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

      // This now calls the updated handler that goes to multiplayer-game-room
      onCreateGame(newRoomCode);
    } catch (err) {
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

      // This now calls the updated handler that goes to multiplayer-game-room
      onJoinGame(roomCode.toUpperCase());
    } catch (err) {
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
              disabled={isCreating || isJoining}
            >
              {isCreating ? 'Creating...' : 'Create Game'}
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
                disabled={isCreating || isJoining}
              />
              <button 
                className="join-button"
                onClick={handleJoinGame}
                disabled={isCreating || isJoining || !roomCode.trim()}
              >
                {isJoining ? 'Joining...' : 'Join Game'}
              </button>
            </div>
          </div>
        </div>

        <button className="back-button" onClick={onBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0L0 8l8 8V0z"/>
          </svg>
          Back to Game Modes
        </button>
      </div>
    </div>
  );
};

export default MultiplayerLobby;
