import React, { useState } from "react";
import "./SelectionModeSelector.css";

const SelectionModeSelector = ({ onSelectMode, onBack }) => {
  const [selectedMode, setSelectedMode] = useState(null);

  const handleModeClick = (mode) => {
    setSelectedMode(mode);
    setTimeout(() => {
      onSelectMode(mode);
    }, 200);
  };

  const modes = [
    {
      id: "category",
      icon: "🗂️",
      name: "By Category",
      description: "Pick a topic and answer questions from that subject area",
      color: "#9cc8ff",
    },
    {
      id: "quiz",
      icon: "📋",
      name: "By Quiz",
      description: "Choose a specific hand-crafted quiz and play it through",
      color: "#a6ffe7",
    },
  ];

  return (
    <div className="selection-mode-container">
      <div className="selection-mode-card">
        <div className="selector-header">
          <h1 className="selector-title">How Do You Want to Play?</h1>
          <p className="selector-subtitle">
            Pick a game style to get started
          </p>
        </div>

        <div className="selection-mode-grid">
          {modes.map((mode) => (
            <button
              key={mode.id}
              className={`selection-mode-option ${selectedMode === mode.id ? "selected" : ""}`}
              onClick={() => handleModeClick(mode.id)}
              style={{ "--mode-color": mode.color }}
            >
              <div className="mode-icon">{mode.icon}</div>
              <div className="mode-content">
                <h3 className="mode-name">{mode.name}</h3>
                <p className="mode-description">{mode.description}</p>
              </div>
              <div className="mode-arrow">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                  <path d="M8 0L0 8l8 8V0z" />
                </svg>
              </div>
            </button>
          ))}
        </div>

        <div className="header-bottom">
          <button className="back-button" onClick={onBack}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z" />
            </svg>
            Back
          </button>
        </div>
      </div>
    </div>
  );
};

export default SelectionModeSelector;
