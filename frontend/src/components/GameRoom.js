import React, { useState, useEffect } from 'react';
import './GameRoom.css';

const GameRoom = ({ roomCode, user, onBack, onStartGame }) => {
  const [players, setPlayers] = useState([user]);
  const [gameState, setGameState] = useState('waiting'); // waiting, in-progress, finished
  const [isHost, setIsHost] = useState(true); // First player is host
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(false);

  // Load categories when component mounts
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const response = await fetch('http://localhost:5216/questions');
        if (response.ok) {
          const questions = await response.json();
          const uniqueCategories = Array.from(new Set(questions.map(q => q.category))).sort();
          setCategories(uniqueCategories);
        }
      } catch (error) {
        console.error('Failed to load categories:', error);
      }
    };

    loadCategories();
  }, []);

  const handleStartGame = async () => {
    if (!selectedCategory) return;
    
    setLoading(true);
    try {
      // Here you would typically start the game via API
      // For now, we'll just call the callback
      onStartGame(selectedCategory);
    } catch (error) {
      console.error('Failed to start game:', error);
    } finally {
      setLoading(false);
    }
  };

  const copyRoomCode = () => {
    navigator.clipboard.writeText(roomCode);
    // You could add a toast notification here
  };

  return (
    <div className="game-room-container">
      <div className="game-room-card">
        <div className="room-header">
          <h2 className="room-title">Game Room</h2>
          <div className="room-code-section">
            <span className="room-code-label">Room Code:</span>
            <div className="room-code-display">
              <span className="room-code">{roomCode}</span>
              <button 
                className="copy-button"
                onClick={copyRoomCode}
                title="Copy room code"
              >
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                  <path d="M4 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V2zm2-1a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H6z"/>
                  <path d="M2 5a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-1h1v1a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h1v1H2z"/>
                </svg>
              </button>
            </div>
          </div>
        </div>

        <div className="players-section">
          <h3>Players ({players.length})</h3>
          <div className="players-list">
            {players.map((player, index) => (
              <div key={player.id || index} className="player-item">
                <div className="player-avatar">
                  {player.username?.charAt(0)?.toUpperCase() || 'P'}
                </div>
                <div className="player-info">
                  <span className="player-name">{player.username}</span>
                  {index === 0 && <span className="host-badge">Host</span>}
                </div>
              </div>
            ))}
          </div>
        </div>

        {gameState === 'waiting' && isHost && (
          <div className="game-setup">
            <h3>Select Category</h3>
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
            
            <button 
              className="start-game-button"
              onClick={handleStartGame}
              disabled={!selectedCategory || loading}
            >
              {loading ? 'Starting...' : 'Start Game'}
            </button>
          </div>
        )}

        {gameState === 'waiting' && !isHost && (
          <div className="waiting-message">
            <div className="waiting-spinner"></div>
            <p>Waiting for host to start the game...</p>
          </div>
        )}

        <button className="back-button" onClick={onBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0L0 8l8 8V0z"/>
          </svg>
          Leave Room
        </button>
      </div>
    </div>
  );
};

export default GameRoom;
