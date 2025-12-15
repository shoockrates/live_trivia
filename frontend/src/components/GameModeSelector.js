import React from 'react';
import './GameModeSelector.css';

const GameModeSelector = ({ onSelectMode, onBack }) => {
  return (
    <div className="game-mode-container">
      <div className="game-mode-card">
        <h2 className="game-mode-title">Choose Game Mode</h2>
        <p className="game-mode-subtitle">How would you like to play?</p>
        
        <div className="game-mode-options">
          <button 
            className="game-mode-option single-player"
            onClick={() => onSelectMode('single')}
          >
            <div className="mode-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
              </svg>
            </div>
            <div className="mode-content">
              <h3>Single Player</h3>
              <p>Practice on your own and improve your knowledge</p>
            </div>
          </button>

          <button 
            className="game-mode-option multiplayer"
            onClick={() => onSelectMode('multiplayer')}
          >
            <div className="mode-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/>
              </svg>
            </div>
            <div className="mode-content">
              <h3>Multiplayer</h3>
              <p>Challenge friends in real-time trivia battles</p>
            </div>
          </button>
        </div>
      </div>
    </div>
  );
};

export default GameModeSelector;
