import React, { useState, useEffect } from 'react';
import './QuestionDisplay.css';

const QuestionDisplay = ({ 
  question, 
  answers, 
  correctIndexes, 
  onAnswerSelect, 
  onNext, 
  currentIndex, 
  totalQuestions,
  correctCount,
  wrongCount,
  revealed,
  questionIn
}) => {
  const [selectedAnswer, setSelectedAnswer] = useState(null);
  const [timeLeft, setTimeLeft] = useState(30);
  const [isAnswered, setIsAnswered] = useState(false);

  useEffect(() => {
    setSelectedAnswer(null);
    setIsAnswered(false);
    setTimeLeft(30);
  }, [currentIndex]);

  useEffect(() => {
    if (!isAnswered && timeLeft > 0) {
      const timer = setTimeout(() => setTimeLeft(timeLeft - 1), 1000);
      return () => clearTimeout(timer);
    } else if (timeLeft === 0 && !isAnswered) {
      handleAnswerSelect(-1); // Time's up
    }
  }, [timeLeft, isAnswered]);

  const handleAnswerSelect = (index) => {
    if (isAnswered) return;
    
    setSelectedAnswer(index);
    setIsAnswered(true);
    onAnswerSelect(index);
  };

  const getAnswerColor = (index) => {
    const colors = [
      { bg: '#2ecc71', hover: '#27ae60' },
      { bg: '#3498db', hover: '#2980b9' },
      { bg: '#e74c3c', hover: '#c0392b' },
      { bg: '#f39c12', hover: '#e67e22' }
    ];
    return colors[index] || colors[0];
  };

  const getAnswerIcon = (index) => {
    const icons = ['A', 'B', 'C', 'D'];
    return icons[index] || '?';
  };

  const progressPercentage = ((currentIndex + (revealed ? 1 : 0)) / totalQuestions) * 100;

  return (
    <div className="question-display-container">
      <div className="question-card">
        {/* Header with progress and timer */}
        <div className="question-header">
          <div className="progress-section">
            <div className="progress-bar">
              <div 
                className="progress-fill"
                style={{ width: `${progressPercentage}%` }}
              />
            </div>
            <div className="progress-text">
              Question {currentIndex + 1} of {totalQuestions}
            </div>
          </div>
          
          <div className="timer-section">
            <div className={`timer ${timeLeft <= 10 ? 'warning' : ''}`}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.5 2,12A10,10 0 0,1 12,2M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z"/>
              </svg>
              {timeLeft}s
            </div>
          </div>
        </div>

        {/* Question text */}
        <div className="question-content">
          <h2 className="question-text">{question}</h2>
        </div>

        {/* Answer options */}
        <div className="answers-container">
          {answers.map((answer, index) => {
            const isSelected = selectedAnswer === index;
            const isCorrect = correctIndexes.includes(index);
            const isWrong = isSelected && !isCorrect;
            const isRevealed = revealed && (isCorrect || isWrong);
            
            const answerColor = getAnswerColor(index);
            const answerIcon = getAnswerIcon(index);
            
            return (
              <button
                key={index}
                className={`answer-option ${isSelected ? 'selected' : ''} ${isRevealed ? (isCorrect ? 'correct' : 'wrong') : ''}`}
                onClick={() => handleAnswerSelect(index)}
                disabled={isAnswered}
                style={{
                  '--answer-color': answerColor.bg,
                  '--answer-hover': answerColor.hover,
                  animationDelay: `${index * 100}ms`
                }}
              >
                <div className="answer-icon">
                  {answerIcon}
                </div>
                <div className="answer-text">
                  {answer}
                </div>
                {isRevealed && (
                  <div className="answer-result">
                    {isCorrect ? (
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
                      </svg>
                    ) : (
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                      </svg>
                    )}
                  </div>
                )}
              </button>
            );
          })}
        </div>

        {/* Stats and controls */}
        <div className="question-footer">
          <div className="stats-section">
            <div className="stat-item correct">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
              </svg>
              <span>{correctCount}</span>
            </div>
            <div className="stat-item wrong">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
              </svg>
              <span>{wrongCount}</span>
            </div>
          </div>

          <button
            className="next-button"
            onClick={onNext}
            disabled={!revealed}
          >
            {currentIndex >= totalQuestions - 1 ? 'Finish Quiz' : 'Next Question'}
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z"/>
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};

export default QuestionDisplay;
