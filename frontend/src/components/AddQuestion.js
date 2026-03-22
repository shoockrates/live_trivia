import React from "react";
import "./AddQuestion.css";

const AddQuestion = ({ onSelectType, onBack }) => {
  return (
    <div className="game-mode-container">
      <div className="game-mode-card">
        <h2 className="game-mode-title">Add A Question/Quiz</h2>
        <p className="game-mode-subtitle">
          Add a question to a specific category or add a new quiz
        </p>

        <div className="game-mode-options">
          <button
            className="game-mode-option single-player"
            onClick={() => onSelectType("add-question")}
          >
            <div className="mode-icon">
              <svg
                width="32"
                height="32"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
              </svg>
            </div>
            <div className="mode-content">
              <h3>Add Question</h3>
            </div>
          </button>
        </div>

        <div className="game-mode-options">
          <button
            className="game-mode-option multiplayer"
            onClick={() => onSelectType("add-quiz")}
          >
            <div className="mode-icon">
              <svg
                width="32"
                height="32"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
              </svg>
            </div>
            <div className="mode-content">
              <h3>Add Quiz</h3>
            </div>
          </button>
        </div>
      </div>
    </div>
  );
};

export default AddQuestion;
