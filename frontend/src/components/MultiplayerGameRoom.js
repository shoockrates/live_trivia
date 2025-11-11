import React, { useState, useEffect, useRef } from 'react';
import './MultiplayerGameRoom.css';
import signalRService from '../services/signalRService';

const MultiplayerGameRoom = ({ roomCode, user, onBack, onStartGame }) => {
  const [players, setPlayers] = useState([]);
  const [gameState, setGameState] = useState('waiting');
  const [isHost, setIsHost] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(false);
  
  // Use refs to prevent duplicate listeners
  const listenersRegistered = useRef(false);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    
    return () => {
      mounted.current = false;
    };
  }, []);

  useEffect(() => {
    let playerJoinedHandler;
    let playerLeftHandler;
    let gameStartedHandler;

    const initializeConnection = async () => {
      try {
        const token = localStorage.getItem('token');
        await signalRService.startConnection(token);
        
        // Define event handlers with proper closures
        playerJoinedHandler = (data) => {
          console.log('Player joined:', data);
          if (!mounted.current) return;
          
          if (data.GameState && data.GameState.players) {
            setPlayers(data.GameState.players);
          } else if (data.Player && !players.find(p => p.id === data.Player.id)) {
            setPlayers(prev => [...prev, data.Player]);
          }
        };

        playerLeftHandler = (data) => {
          console.log('Player left:', data);
          if (!mounted.current) return;
          
          setPlayers(prev => prev.filter(p => 
            p.connectionId !== data.PlayerId && 
            p.id !== data.PlayerId
          ));
        };

        gameStartedHandler = (gameDetails) => {
          console.log('Game started:', gameDetails);
          if (!mounted.current) return;
          
          setGameState('in-progress');
          onStartGame(selectedCategory, roomCode);
        };

        // Register listeners only once
        if (!listenersRegistered.current) {
          signalRService.onPlayerJoined(playerJoinedHandler);
          signalRService.onPlayerLeft(playerLeftHandler);
          signalRService.onGameStarted(gameStartedHandler);
          listenersRegistered.current = true;
        }

        // Join the room
        await signalRService.joinGameRoom(roomCode);

        // Load initial game state
        await loadGameState();

      } catch (error) {
        console.error('Failed to initialize SignalR connection:', error);
      }
    };

    const loadGameState = async () => {
      try {
        const response = await fetch(`http://localhost:5216/games/${roomCode}`, {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        
        if (response.ok) {
          const gameDetails = await response.json();
          if (mounted.current) {
            setIsHost(gameDetails.hostPlayerId === parseInt(user.playerId));
            setPlayers(gameDetails.players || []);
          }
        }
      } catch (error) {
        console.error('Failed to load game details:', error);
      }
    };

    initializeConnection();

    // Cleanup function
    return () => {
      if (listenersRegistered.current) {
        // Remove the specific listeners
        if (playerJoinedHandler) {
          signalRService.removeListener('PlayerJoined', playerJoinedHandler);
        }
        if (playerLeftHandler) {
          signalRService.removeListener('PlayerLeft', playerLeftHandler);
        }
        if (gameStartedHandler) {
          signalRService.removeListener('GameStarted', gameStartedHandler);
        }
        
        listenersRegistered.current = false;
      }
      
      // Leave the room
      signalRService.leaveGameRoom(roomCode).catch(err => 
        console.error('Error leaving room:', err)
      );
    };
  }, [roomCode, user.playerId, selectedCategory, onStartGame]);

  // Load categories when component mounts
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const response = await fetch('http://localhost:5216/questions', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        
        if (response.ok) {
          const questions = await response.json();
          const uniqueCategories = Array.from(new Set(questions.map(q => q.category))).sort();
          if (mounted.current) {
            setCategories(uniqueCategories);
          }
        }
      } catch (error) {
        console.error('Failed to load categories:', error);
      }
    };

    loadCategories();
  }, []);

  const handleStartGame = async () => {
    if (!selectedCategory) {
      alert('Please select a category first');
      return;
    }
    
    setLoading(true);
    try {
      const response = await fetch(`http://localhost:5216/games/${roomCode}/start`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to start game');
      }
      // The actual game start will be handled via SignalR event
    } catch (error) {
      console.error('Failed to start game:', error);
      alert('Failed to start game: ' + error.message);
    } finally {
      if (mounted.current) {
        setLoading(false);
      }
    }
  };

  const copyRoomCode = () => {
    navigator.clipboard.writeText(roomCode);
    // Simple feedback - you can replace with a toast notification
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
    // Cleanup will happen in useEffect cleanup
    onBack();
  };

  return (
    <div className="multiplayer-game-room-container">
      <div className="multiplayer-game-room-card">
        <div className="room-header">
          <h2 className="room-title">Multiplayer Game Room</h2>
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
            {players.length === 0 ? (
              <p className="no-players">Waiting for players...</p>
            ) : (
              players.map((player, index) => (
                <div key={player.playerId || player.id || index} className="player-item">
                  <div className="player-avatar">
                    {(player.username?.charAt(0) || player.name?.charAt(0) || 'P').toUpperCase()}
                  </div>
                  <div className="player-info">
                    <span className="player-name">{player.username || player.name}</span>
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

        {gameState === 'waiting' && isHost && (
          <div className="game-setup">
            <h3>Game Settings</h3>
            <div className="category-selection">
              <label>Select Category:</label>
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
            </div>
            
            <button 
              className="start-game-button"
              onClick={handleStartGame}
              disabled={!selectedCategory || loading || players.length < 1}
            >
              {loading ? 'Starting...' : `Start Game (${players.length} player${players.length !== 1 ? 's' : ''})`}
            </button>
            
            {players.length < 2 && (
              <p className="info-message">Tip: Share the room code with friends to play together!</p>
            )}
          </div>
        )}

        {gameState === 'waiting' && !isHost && (
          <div className="waiting-message">
            <div className="waiting-spinner"></div>
            <p>Waiting for host to start the game...</p>
            <p>Players in room: {players.length}</p>
          </div>
        )}

        <button className="back-button" onClick={handleBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path fillRule="evenodd" d="M15 8a.5.5 0 0 0-.5-.5H2.707l3.147-3.146a.5.5 0 1 0-.708-.708l-4 4a.5.5 0 0 0 0 .708l4 4a.5.5 0 0 0 .708-.708L2.707 8.5H14.5A.5.5 0 0 0 15 8z"/>
          </svg>
          Leave Room
        </button>
      </div>
    </div>
  );
};

export default MultiplayerGameRoom;
