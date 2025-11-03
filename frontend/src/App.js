import React, { useEffect, useMemo, useState } from 'react';
import './App.css';
import Login from './components/Login';
import Register from './components/Register';
import UserDropdown from './components/UserDropdown';
import GameModeSelector from './components/GameModeSelector';
import MultiplayerLobby from './components/MultiplayerLobby';
import GameRoom from './components/GameRoom';
import PlayerStats from './components/PlayerStats';
import Leaderboard from './components/Leaderboard';
import CategorySelector from './components/CategorySelector';
import QuestionDisplay from './components/QuestionDisplay';
import GameResults from './components/GameResults';
import Profile from './components/Profile';

function App() {
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [categories, setCategories] = useState([]);
  const [questions, setQuestions] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [isAnimatingBack, setIsAnimatingBack] = useState(false);
  const [selectedAnswerIndex, setSelectedAnswerIndex] = useState(null);
  const [revealed, setRevealed] = useState(false);
  const [correctCount, setCorrectCount] = useState(0);
  const [wrongCount, setWrongCount] = useState(0);
  const [gameStartedAt, setGameStartedAt] = useState(null);
  const [gameEndedAt, setGameEndedAt] = useState(null);
  const [gameFinished, setGameFinished] = useState(false);
  const [questionIn, setQuestionIn] = useState(false);
  const [resultsIn, setResultsIn] = useState(false);


  
  // Authentication states
  const [currentView, setCurrentView] = useState('auth');
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  
  // Game mode states
  const [gameMode, setGameMode] = useState(null); // 'single' or 'multiplayer'
  const [roomCode, setRoomCode] = useState(null);
  const [currentGameRoom, setCurrentGameRoom] = useState(null);
  const [showStats, setShowStats] = useState(false);
  const [showLeaderboard, setShowLeaderboard] = useState(false);
  const [showProfile, setShowProfile] = useState(false);

  // Backend base URL
  const API_BASE = useMemo(() => 'http://localhost:5216', []);

  // Check if user is already logged in on app start
  useEffect(() => {
    const token = localStorage.getItem('token');
    const username = localStorage.getItem('username');
    const playerId = localStorage.getItem('playerId');

    if (token && username) {
      setUser({ username, playerId, token });
      setIsAuthenticated(true);
      setCurrentView('game-mode');
    }
  }, []);

  // Add authentication header to all fetch requests
  useEffect(() => {
    const originalFetch = window.fetch;
    window.fetch = async (...args) => {
      const [url, options = {}] = args;
      const token = localStorage.getItem('token');
      
      if (token && url.startsWith(API_BASE)) {
        options.headers = {
          ...options.headers,
          'Authorization': `Bearer ${token}`
        };
      }
      
      return originalFetch(url, options);
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, [API_BASE]);

  // Authentication handlers
  const handleLogin = (userData) => {
    setUser(userData);
    setIsAuthenticated(true);
    setCurrentView('game-mode');
  };

  const handleRegister = (userData) => {
    setUser(userData);
    setIsAuthenticated(true);
    setCurrentView('game-mode');
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('playerId');
    setUser(null);
    setIsAuthenticated(false);
    setCurrentView('auth');
    setSelectedCategory(null);
    setGameMode(null);
    setRoomCode(null);
    setCurrentGameRoom(null);
  };

  const showLogin = () => setCurrentView('login');
  const showRegister = () => setCurrentView('register');
  const showAuth = () => setCurrentView('auth');
  
  // Game mode handlers
  const handleGameModeSelect = (mode) => {
    setGameMode(mode);
    if (mode === 'single') {
      setCurrentView('game');
    } else if (mode === 'multiplayer') {
      setCurrentView('multiplayer-lobby');
    }
  };
  
  const handleBackToGameMode = () => {
    setCurrentView('game-mode');
    setGameMode(null);
    setRoomCode(null);
    setCurrentGameRoom(null);
  };
  
  const handleCreateGame = (newRoomCode) => {
    setRoomCode(newRoomCode);
    setCurrentGameRoom({ roomCode: newRoomCode, isHost: true });
    setCurrentView('game-room');
  };
  
  const handleJoinGame = (joinedRoomCode) => {
    setRoomCode(joinedRoomCode);
    setCurrentGameRoom({ roomCode: joinedRoomCode, isHost: false });
    setCurrentView('game-room');
  };
  
  const handleBackToLobby = () => {
    setCurrentView('multiplayer-lobby');
    setRoomCode(null);
    setCurrentGameRoom(null);
  };
  
  const handleStartMultiplayerGame = (category) => {
    setSelectedCategory(category);
    setCurrentView('game');
  };
  
  const handleShowStats = () => {
    console.log('Showing statistics');
    setShowStats(true);
  };
  
  const handleBackFromStats = () => {
    setShowStats(false);
    if(gameMode === 'single') {
            setCurrentView('game');
        } else if (gameMode === 'multiplayer-lobby'){
            setCurrentView('game-room');
        } else {
            setCurrentView('game-mode')
        }
  };
  
  const handleShowLeaderboard = () => {
    setShowLeaderboard(true);
  };
  
  const handleBackFromLeaderboard = () => {
    setShowLeaderboard(false);
    if(gameMode === 'single') {
            setCurrentView('game');
        } else if (gameMode === 'multiplayer-lobby'){
            setCurrentView('game-room');
        } else {
            setCurrentView('game-mode')
        }
  };

  const handleShowProfile = () => {
    console.log('Showing profile, current gameMode:', gameMode);
    setShowProfile(true);
  };

  const handleBackFromProfile = () => {
    setShowProfile(false);
  };

  // Load all questions once to derive categories
  useEffect(() => {
    if (!isAuthenticated) return;

    const controller = new AbortController();
    const load = async () => {
      try {
        setError(null);
        const res = await fetch(`${API_BASE}/questions`, { signal: controller.signal });
        if (!res.ok) throw new Error(`Failed to load questions (${res.status})`);
        const all = await res.json();
        const valid = (Array.isArray(all) ? all : []).filter(q => {
          const t = (q.text || q.question || '').trim();
          const a = Array.isArray(q.answers || q.Answers) ? (q.answers || q.Answers) : [];
          const c = Array.isArray(q.correctAnswerIndexes || q.CorrectAnswerIndexes) ? (q.correctAnswerIndexes || q.CorrectAnswerIndexes) : [];
          return t.length > 0 && a.length > 0 && c.length > 0;
        });
        const unique = Array.from(new Set(valid.map(q => q.category))).sort((a, b) => a.localeCompare(b));
        setCategories(unique);
      } catch (e) {
        if (e.name !== 'AbortError') setError(e.message || 'Failed to load');
      }
    };
    load();
    return () => controller.abort();
  }, [API_BASE, isAuthenticated]);

  const handleSelect = (category) => {
    setSelectedCategory(category);
    setLoading(true);
    setError(null);
    setCurrentIndex(0);
    setSelectedAnswerIndex(null);
    setRevealed(false);
    setCorrectCount(0);
    setWrongCount(0);
    setGameFinished(false);
    setGameEndedAt(null);
    setGameStartedAt(Date.now());
    
    fetch(`${API_BASE}/questions/category/${encodeURIComponent(category)}`)
      .then(r => {
        if (!r.ok) throw new Error(`Failed to load ${category} (${r.status})`);
        return r.json();
      })
      .then(data => {
        const arr = Array.isArray(data) ? data : [];
        const filtered = arr.filter(q => {
          const t = (q.text || q.question || '').trim();
          const a = Array.isArray(q.answers || q.Answers) ? (q.answers || q.Answers) : [];
          const c = Array.isArray(q.correctAnswerIndexes || q.CorrectAnswerIndexes) ? (q.correctAnswerIndexes || q.CorrectAnswerIndexes) : [];
          return t.length > 0 && a.length > 0 && c.length > 0;
        });
        setQuestions(filtered);
      })
      .catch(err => setError(err.message || 'Failed to load'))
      .finally(() => setLoading(false));
  };

  const handleBack = () => {
    setIsAnimatingBack(true);
    setTimeout(() => {
      setSelectedCategory(null);
      setIsAnimatingBack(false);
    }, 220);
  };

  const currentQuestion = questions[currentIndex] || {};
  const questionText = (currentQuestion.text || currentQuestion.question || '').trim();
  const rawAnswers = currentQuestion.answers || currentQuestion.Answers || [];
  const answers = Array.isArray(rawAnswers) ? rawAnswers.slice(0, 4) : [];
  const correctIdxs = useMemo(() => {
    const arr = currentQuestion.correctAnswerIndexes || currentQuestion.CorrectAnswerIndexes || [];
    return Array.isArray(arr) ? arr : [];
  }, [currentQuestion]);

  // Trigger enter animation on question change
  useEffect(() => {
    setQuestionIn(false);
    const t = setTimeout(() => setQuestionIn(true), 20);
    return () => clearTimeout(t);
  }, [currentIndex, selectedCategory]);

  // Trigger enter animation for results view
  useEffect(() => {
    if (gameFinished) {
      setResultsIn(false);
      const t = setTimeout(() => setResultsIn(true), 20);
      return () => clearTimeout(t);
    } else {
      setResultsIn(false);
    }
  }, [gameFinished]);

  const selectAnswer = (idx) => {
    if (revealed) return;
    setSelectedAnswerIndex(idx);
    const isCorrect = correctIdxs.includes(idx);
    if (isCorrect) {
      setCorrectCount(c => c + 1);
    } else {
      setWrongCount(w => w + 1);
    }
    setRevealed(true);
  };

  const goNext = () => {
    if (currentIndex >= questions.length - 1) {
      if (!gameFinished) {
        setGameFinished(true);
        setGameEndedAt(Date.now());
        updateGameStatistics(selectedCategory, correctCount, questions.length, calculateScore());
      }
      return;
    }
    setCurrentIndex(i => i + 1);
    setSelectedAnswerIndex(null);
    setRevealed(false);
  };

  const restartGame = () => {
    if (!selectedCategory) return;
    handleSelect(selectedCategory);
  };

  const percent = useMemo(() => {
    const total = questions.length || 0;
    if (total === 0) return 0;
    return Math.round((correctCount / total) * 100);
  }, [correctCount, questions.length]);

  const getGrade = (pct, mistakes) => {
    if (mistakes === 0) return 'A+';
    if (pct >= 90) return 'A';
    if (pct >= 80) return 'B';
    if (pct >= 70) return 'C';
    if (pct >= 60) return 'D';
    return 'F';
  };

  const gameDurationMs = gameStartedAt && gameEndedAt ? (gameEndedAt - gameStartedAt) : null;
  const formatDuration = (ms) => {
    if (!ms || ms < 0) return '0:00';
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };
    

  const updateGameStatistics = async (category, correctAnswers, totalQuestions, score) => {
  try {
    const response = await fetch(`${API_BASE}/statistics/update`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      },
      body: JSON.stringify({
        category,
        score,
        correctAnswers,
        totalQuestions
      })
    });

    if (!response.ok) {
      console.error('Failed to update statistics');
    }
  } catch (error) {
    console.error('Error updating statistics:', error);
  }
};

const calculateScore = () => {
  // Simple scoring 
  return Math.round((correctCount / questions.length) * 100);
};

  // Render authentication views
  if (!isAuthenticated) {
    return (
      <div className="App">
        {currentView === 'auth' && (
          <div className="TriviaContainer">
            <div className="HeroCard">
              <h1 className="HeroTitle">Live Trivia</h1>
              <p className="HeroSubtitle">Test your knowledge and challenge friends</p>
              <div className="ButtonsGrid">
                <button 
                  className="TriviaButton gradient" 
                  onClick={showLogin}
                >
                  Sign In
                </button>
                <button 
                  className="TriviaButton" 
                  onClick={showRegister}
                >
                  Create Account
                </button>
              </div>
            </div>
          </div>
        )}
        
        {currentView === 'login' && (
          <Login 
            onLogin={handleLogin}
            onSwitchToRegister={showRegister}
          />
        )}
        
        {currentView === 'register' && (
          <Register 
            onRegister={handleRegister}
            onSwitchToLogin={showLogin}
          />
        )}
      </div>
    );
  }
  
  // Render player profile
  if (showProfile) {
        return (
          <div className="App">
            <Profile
              user={user}
              onBack={handleBackFromProfile}
            />
          </div>
        );
  }

  // Render player statistics
  if (showStats) {
    return (
      <div className="App">
        <PlayerStats 
          user={user}
          onBack={handleBackFromStats}
        />
      </div>
    );
  }

  
  // Render leaderboard
  if (showLeaderboard) {
    return (
      <div className="App">
        <Leaderboard 
          onBack={handleBackFromLeaderboard}
        />
      </div>
    );
  }

  

  // Render game mode selector
  if (currentView === 'game-mode') {
    return (
      <div className="App">
        <div style={{ 
          position: 'absolute', 
          top: '20px', 
          right: '20px', 
          zIndex: 1000 
        }}>
          <UserDropdown 
            user={user} 
            onLogout={handleLogout}
            onShowStats={handleShowStats}
            onShowLeaderboard={handleShowLeaderboard}
            onShowProfile={handleShowProfile}
          />
        </div>
        <GameModeSelector 
          onSelectMode={handleGameModeSelect}
          onBack={handleLogout}
        />
      </div>
    );
  }

  // Render multiplayer lobby
  if (currentView === 'multiplayer-lobby') {
    return (
      <div className="App">
        <MultiplayerLobby 
          onBack={handleBackToGameMode}
          onCreateGame={handleCreateGame}
          onJoinGame={handleJoinGame}
          user={user}
        />
      </div>
    );
  }

  // Render game room
  if (currentView === 'game-room') {
    return (
      <div className="App">
        <GameRoom 
          roomCode={roomCode}
          user={user}
          onBack={handleBackToLobby}
          onStartGame={handleStartMultiplayerGame}
        />
      </div>
    );
  }

  // Render main game for authenticated users
  return (
    <div className="App">
      <div className="TriviaContainer">
        {/* Logout button */}
        <div style={{ 
            position: 'absolute', 
            top: '20px', 
            right: '20px', 
            zIndex: 1000 
            }}>
            <UserDropdown 
                user={user} 
                onLogout={handleLogout} 
                onShowStats={handleShowStats}
                onShowLeaderboard={handleShowLeaderboard}
                onShowProfile={handleShowProfile}
            />
            </div>

        {!selectedCategory && (
          <CategorySelector 
            categories={categories}
            onSelectCategory={handleSelect}
            loading={loading}
            error={error}
            user={user}
            onBack={handleBackToGameMode}
          />
        )}

        {selectedCategory && (
          <>
            {loading && (
              <div className="loading-container">
                <div className="loading-spinner"></div>
                <p>Loading questions…</p>
              </div>
            )}
            {!loading && error && (
              <div className="error-container">
                <p style={{ color: '#ff6b6b' }}>{error}</p>
              </div>
            )}
            {!loading && !error && questions.length === 0 && (
              <div className="error-container">
                <p>No questions found for "{selectedCategory}".</p>
              </div>
            )}
            {!loading && !error && questions.length > 0 && !gameFinished && (
              <QuestionDisplay
                question={questionText}
                answers={answers}
                correctIndexes={correctIdxs}
                onAnswerSelect={selectAnswer}
                onNext={goNext}
                currentIndex={currentIndex}
                totalQuestions={questions.length}
                correctCount={correctCount}
                wrongCount={wrongCount}
                revealed={revealed}
                questionIn={questionIn}
              />
            )}
            {!loading && !error && questions.length > 0 && gameFinished && (
              <GameResults
                correctCount={correctCount}
                wrongCount={wrongCount}
                totalQuestions={questions.length}
                gameDuration={gameDurationMs}
                onPlayAgain={restartGame}
                onBackToCategories={handleBack}
                category={selectedCategory}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}

export default App;
