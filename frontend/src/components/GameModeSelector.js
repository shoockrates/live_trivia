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
                <path d="M16 4c0-1.11.89-2 2-2s2 .89 2 2-.89 2-2 2-2-.89-2-2zm4 18v-6h2.5l-2.54-7.63A1.5 1.5 0 0 0 18.54 8H17c-.8 0-1.54.37-2.01.99L12 14l-2.99-5.01A2.5 2.5 0 0 0 7 8H5.46c-.8 0-1.54.37-2.01.99L.96 16.5H3.5V22h2v-6h2.5l2.04 6.13A1.5 1.5 0 0 0 11.46 22H12.5c.8 0 1.54-.37 2.01-.99L16.5 16H19v6h2z"/>
              </svg>
            </div>
            <div className="mode-content">
              <h3>Multiplayer</h3>
              <p>Challenge friends in real-time trivia battles</p>
            </div>
          </button>
        </div>

        <button className="back-button" onClick={onBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0L0 8l8 8V0z"/>
          </svg>
          Back
        </button>
      </div>
    </div>
  );
};

export default GameModeSelector;
