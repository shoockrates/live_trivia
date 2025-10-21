import React, { useState, useEffect } from 'react';
import './Leaderboard.css';

const Leaderboard = ({ onBack }) => {
  const [leaderboard, setLeaderboard] = useState([]);
  const [loading, setLoading] = useState(true);
  const [timeFilter, setTimeFilter] = useState('all'); // all, week, month

  useEffect(() => {
    // Mock data for now - in a real app, this would fetch from the API
    const mockLeaderboard = [
      { rank: 1, username: 'TriviaMaster', score: 2450, gamesPlayed: 15, accuracy: 92 },
      { rank: 2, username: 'QuizWizard', score: 2380, gamesPlayed: 12, accuracy: 89 },
      { rank: 3, username: 'BrainBox', score: 2250, gamesPlayed: 18, accuracy: 87 },
      { rank: 4, username: 'KnowledgeKing', score: 2100, gamesPlayed: 14, accuracy: 85 },
      { rank: 5, username: 'SmartPlayer', score: 1980, gamesPlayed: 11, accuracy: 83 },
      { rank: 6, username: 'QuizChamp', score: 1850, gamesPlayed: 16, accuracy: 81 },
      { rank: 7, username: 'TriviaPro', score: 1720, gamesPlayed: 13, accuracy: 79 },
      { rank: 8, username: 'BrainMaster', score: 1650, gamesPlayed: 10, accuracy: 77 },
      { rank: 9, username: 'QuizExpert', score: 1580, gamesPlayed: 9, accuracy: 75 },
      { rank: 10, username: 'TriviaGuru', score: 1420, gamesPlayed: 8, accuracy: 73 }
    ];
    
    setTimeout(() => {
      setLeaderboard(mockLeaderboard);
      setLoading(false);
    }, 1000);
  }, [timeFilter]);

  const getRankIcon = (rank) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  };

  const getRankClass = (rank) => {
    if (rank <= 3) return 'top-three';
    if (rank <= 10) return 'top-ten';
    return 'other';
  };

  if (loading) {
    return (
      <div className="leaderboard-container">
        <div className="leaderboard-card">
          <div className="loading-spinner"></div>
          <p>Loading leaderboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="leaderboard-container">
      <div className="leaderboard-card">
        <div className="leaderboard-header">
          <h2 className="leaderboard-title">Leaderboard</h2>
          <p className="leaderboard-subtitle">Top trivia players</p>
          
          <div className="filter-tabs">
            <button 
              className={`filter-tab ${timeFilter === 'all' ? 'active' : ''}`}
              onClick={() => setTimeFilter('all')}
            >
              All Time
            </button>
            <button 
              className={`filter-tab ${timeFilter === 'week' ? 'active' : ''}`}
              onClick={() => setTimeFilter('week')}
            >
              This Week
            </button>
            <button 
              className={`filter-tab ${timeFilter === 'month' ? 'active' : ''}`}
              onClick={() => setTimeFilter('month')}
            >
              This Month
            </button>
          </div>
        </div>

        <div className="leaderboard-list">
          {leaderboard.map((player) => (
            <div key={player.rank} className={`leaderboard-item ${getRankClass(player.rank)}`}>
              <div className="rank-section">
                <div className="rank-icon">{getRankIcon(player.rank)}</div>
                <div className="rank-number">{player.rank}</div>
              </div>
              
              <div className="player-info">
                <div className="player-avatar">
                  {player.username.charAt(0).toUpperCase()}
                </div>
                <div className="player-details">
                  <div className="player-name">{player.username}</div>
                  <div className="player-stats">
                    {player.gamesPlayed} games • {player.accuracy}% accuracy
                  </div>
                </div>
              </div>
              
              <div className="score-section">
                <div className="score-value">{player.score.toLocaleString()}</div>
                <div className="score-label">points</div>
              </div>
            </div>
          ))}
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

export default Leaderboard;
