import React from 'react';
import './GameResults.css';

const GameResults = ({ 
  correctCount, 
  wrongCount, 
  totalQuestions, 
  gameDuration, 
  onPlayAgain, 
  onBackToCategories,
  category 
}) => {
  const accuracy = totalQuestions > 0 ? Math.round((correctCount / totalQuestions) * 100) : 0;
  
  const getGrade = (accuracy) => {
    if (accuracy >= 95) return { grade: 'A+', color: '#2ecc71', message: 'Outstanding!' };
    if (accuracy >= 90) return { grade: 'A', color: '#2ecc71', message: 'Excellent!' };
    if (accuracy >= 80) return { grade: 'B', color: '#f39c12', message: 'Great job!' };
    if (accuracy >= 70) return { grade: 'C', color: '#e67e22', message: 'Good work!' };
    if (accuracy >= 60) return { grade: 'D', color: '#e74c3c', message: 'Keep trying!' };
    return { grade: 'F', color: '#e74c3c', message: 'Practice more!' };
  };

  const getPerformanceMessage = (accuracy) => {
    if (accuracy >= 90) return "You're a trivia master! 🏆";
    if (accuracy >= 80) return "Great performance! 🌟";
    if (accuracy >= 70) return "Well done! 👍";
    if (accuracy >= 60) return "Not bad! Keep practicing! 💪";
    return "Don't give up! Try again! 🔄";
  };

  const gradeInfo = getGrade(accuracy);
  const performanceMessage = getPerformanceMessage(accuracy);

  const formatTime = (ms) => {
    if (!ms) return '0:00';
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  return (
    <div className="results-container">
      <div className="results-card">
        <div className="results-header">
          <div className="category-badge">
            <span className="category-icon">🧠</span>
            <span className="category-name">{category}</span>
          </div>
          <h1 className="results-title">Quiz Complete!</h1>
          <p className="results-subtitle">{performanceMessage}</p>
        </div>

        <div className="results-grid">
          <div className="result-card accuracy">
            <div className="result-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
              </svg>
            </div>
            <div className="result-content">
              <div className="result-value">{accuracy}%</div>
              <div className="result-label">Accuracy</div>
            </div>
          </div>

          <div className="result-card correct">
            <div className="result-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
              </svg>
            </div>
            <div className="result-content">
              <div className="result-value">{correctCount}</div>
              <div className="result-label">Correct</div>
            </div>
          </div>

          <div className="result-card wrong">
            <div className="result-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
            </div>
            <div className="result-content">
              <div className="result-value">{wrongCount}</div>
              <div className="result-label">Incorrect</div>
            </div>
          </div>

          <div className="result-card time">
            <div className="result-icon">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.5 2,12A10,10 0 0,1 12,2M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z"/>
              </svg>
            </div>
            <div className="result-content">
              <div className="result-value">{formatTime(gameDuration)}</div>
              <div className="result-label">Time</div>
            </div>
          </div>
        </div>

        <div className="grade-section">
          <div className="grade-card" style={{ '--grade-color': gradeInfo.color }}>
            <div className="grade-icon">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <div className="grade-content">
              <div className="grade-value" style={{ color: gradeInfo.color }}>
                {gradeInfo.grade}
              </div>
              <div className="grade-message">{gradeInfo.message}</div>
            </div>
          </div>
        </div>

        <div className="results-actions">
          <button className="action-button secondary" onClick={onBackToCategories}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z"/>
            </svg>
            Back to Categories
          </button>
          
          <button className="action-button primary" onClick={onPlayAgain}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
              <path d="M17.65,6.35C16.2,4.9 14.21,4 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20C15.73,20 18.84,17.45 19.73,14H17.65C16.83,16.33 14.61,18 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6C13.66,6 15.14,6.69 16.22,7.78L13,11H20V4L17.65,6.35Z"/>
            </svg>
            Play Again
          </button>
        </div>
      </div>
    </div>
  );
};

export default GameResults;
