import React, { useState, useEffect } from 'react';
import './PlayerStats.css';



const PlayerStats = ({ user, onBack }) => {
  const [stats, setStats] = useState({
    totalGames: 0,
    totalCorrect: 0,
    totalQuestions: 0,
    averageScore: 0,
    bestScore: 0,
    categoriesPlayed: []
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
  const fetchStats = async () => {
    try {
      setLoading(true);
      const response = await fetch('http://localhost:5216/statistics/player', {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      
      if (response.ok) {
        const realStats = await response.json();
        console.log('Fetched stats:', realStats);
        
        setStats({
          totalGames: realStats.totalGamesPlayed,
          totalCorrect: realStats.totalCorrectAnswers,
          totalQuestions: realStats.totalQuestionsAnswered,
          averageScore: realStats.averageScore,
          bestScore: realStats.bestScore,
          categoriesPlayed: realStats.categoryStats?.map(cat => cat.category) || []
        });
      } else {
        const errorText = await response.text();
        console.error('Failed to fetch statistics:', errorText);
      }
    } catch (error) {
      console.error('Error fetching statistics:', error);
    } finally {
      setLoading(false);
    }
  };

  fetchStats();
}, []);
  const accuracy = stats.totalQuestions > 0 ? Math.round((stats.totalCorrect / stats.totalQuestions) * 100) : 0;

  if (loading) {
    return (
      <div className="stats-container">
        <div className="stats-card">
          <div className="loading-spinner"></div>
          <p>Loading your statistics...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="stats-container">
      <div className="stats-card">
        <div className="stats-header">
          <h2 className="stats-title">Your Statistics</h2>
          <p className="stats-subtitle">Track your trivia progress</p>
        </div>

        <div className="stats-grid">
          <div className="stat-item">
            <div className="stat-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <div className="stat-content">
              <div className="stat-value">{stats.totalGames}</div>
              <div className="stat-label">Games Played</div>
            </div>
          </div>

          <div className="stat-item">
            <div className="stat-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
              </svg>
            </div>
            <div className="stat-content">
              <div className="stat-value">{stats.totalCorrect}</div>
              <div className="stat-label">Correct Answers</div>
            </div>
          </div>

          <div className="stat-item">
            <div className="stat-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
              </svg>
            </div>
            <div className="stat-content">
              <div className="stat-value">{accuracy}%</div>
              <div className="stat-label">Accuracy</div>
            </div>
          </div>

          <div className="stat-item">
            <div className="stat-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <div className="stat-content">
              <div className="stat-value">{stats.bestScore}</div>
              <div className="stat-label">Best Score</div>
            </div>
          </div>
        </div>

        <div className="categories-section">
          <h3>Categories Played</h3>
          <div className="categories-list">
            {stats.categoriesPlayed.map((category, index) => (
              <div key={index} className="category-tag">
                {category}
              </div>
            ))}
          </div>
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

export default PlayerStats;
