import React, { useState, useEffect } from "react";
import "./CategorySelector.css"; // reuse exact same styles

const QuizSelector = ({ quizzes, onSelectQuiz, loading, error, onBack }) => {
  const [searchTerm, setSearchTerm] = useState("");
  const [filteredQuizzes, setFilteredQuizzes] = useState(quizzes);
  const [selectedQuiz, setSelectedQuiz] = useState(null);

  useEffect(() => {
    if (searchTerm.trim() === "") {
      setFilteredQuizzes(quizzes);
    } else {
      setFilteredQuizzes(
        quizzes.filter((quiz) =>
          quiz.toLowerCase().includes(searchTerm.toLowerCase())
        )
      );
    }
  }, [searchTerm, quizzes]);

  const handleQuizClick = (quiz) => {
    setSelectedQuiz(quiz);
    setTimeout(() => {
      onSelectQuiz(quiz);
    }, 200);
  };

  // Generate a consistent colour per quiz name
  const getQuizColor = (name) => {
    const palette = [
      "#9cc8ff", "#a6ffe7", "#6e7bff", "#e91e63", "#ff9800",
      "#2ecc71", "#9b59b6", "#3498db", "#f39c12", "#e74c3c",
      "#00bcd4", "#8bc34a", "#673ab7", "#607d8b", "#ffc107",
    ];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return palette[Math.abs(hash) % palette.length];
  };

  const getQuizIcon = (name) => {
    const lower = name.toLowerCase();
    if (lower.includes("science")) return "🔬";
    if (lower.includes("history")) return "📚";
    if (lower.includes("geo")) return "🌍";
    if (lower.includes("sport")) return "⚽";
    if (lower.includes("movie") || lower.includes("film")) return "🎬";
    if (lower.includes("music")) return "🎵";
    if (lower.includes("math")) return "🔢";
    if (lower.includes("tech")) return "💻";
    if (lower.includes("art")) return "🎨";
    if (lower.includes("food")) return "🍕";
    if (lower.includes("space") || lower.includes("astro")) return "🚀";
    if (lower.includes("nature") || lower.includes("animal")) return "🌿";
    return "📋";
  };

  return (
    <div className="category-selector-container">
      <div className="category-selector-card">
        <div className="selector-header">
          <h1 className="selector-title">Choose a Quiz</h1>
          <p className="selector-subtitle">
            Select a quiz to test your knowledge
          </p>

          <div className="search-container">
            <div className="search-input-wrapper">
              <svg
                className="search-icon"
                width="20"
                height="20"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 14z" />
              </svg>
              <input
                type="text"
                placeholder="Search quizzes..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="search-input"
              />
            </div>
          </div>
        </div>

        {error && (
          <div className="error-message">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
            </svg>
            {error}
          </div>
        )}

        {loading && (
          <div className="loading-container">
            <div className="loading-spinner"></div>
            <p>Loading quizzes...</p>
          </div>
        )}

        {!loading && !error && (
          <div className="categories-grid">
            {filteredQuizzes.map((quiz, index) => (
              <button
                key={quiz}
                className={`category-card ${selectedQuiz === quiz ? "selected" : ""}`}
                onClick={() => handleQuizClick(quiz)}
                style={{
                  "--category-color": getQuizColor(quiz),
                  animationDelay: `${index * 50}ms`,
                }}
              >
                <div className="category-icon">{getQuizIcon(quiz)}</div>
                <div className="category-content">
                  <h3 className="category-name">{quiz}</h3>
                  <p className="category-description">
                    Play the {quiz} quiz
                  </p>
                </div>
                <div className="category-arrow">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <path d="M8 0L0 8l8 8V0z" />
                  </svg>
                </div>
              </button>
            ))}
          </div>
        )}

        <div className="header-bottom">
          <button className="back-button" onClick={onBack}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z" />
            </svg>
            Back
          </button>
        </div>

        {!loading && !error && filteredQuizzes.length === 0 && searchTerm && (
          <div className="no-results">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="currentColor">
              <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 14z" />
            </svg>
            <h3>No quizzes found</h3>
            <p>Try searching for something else</p>
          </div>
        )}

        {!loading && !error && filteredQuizzes.length === 0 && !searchTerm && (
          <div className="no-results">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="currentColor">
              <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 14z" />
            </svg>
            <h3>No quizzes available</h3>
            <p>Check back soon or add a new quiz</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default QuizSelector;
