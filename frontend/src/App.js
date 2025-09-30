import React, { useEffect, useMemo, useState } from 'react';
import './App.css';

function App() {
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [categories, setCategories] = useState([]);
  const [questions, setQuestions] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [isAnimatingBack, setIsAnimatingBack] = useState(false);

  // Backend base URL
  const API_BASE = useMemo(() => 'http://localhost:5216', []);

  // Load all questions once to derive categories
  useEffect(() => {
    const controller = new AbortController();
    const load = async () => {
      try {
        setError(null);
        const res = await fetch(`${API_BASE}/questions`, { signal: controller.signal });
        if (!res.ok) throw new Error(`Failed to load questions (${res.status})`);
        const all = await res.json();
        const unique = Array.from(new Set(all.map(q => q.category))).sort((a, b) => a.localeCompare(b));
        setCategories(unique);
      } catch (e) {
        if (e.name !== 'AbortError') setError(e.message || 'Failed to load');
      }
    };
    load();
    return () => controller.abort();
  }, [API_BASE]);

  const handleSelect = (category) => {
    setSelectedCategory(category);
    // fetch questions for the selected category
    setLoading(true);
    setError(null);
    setCurrentIndex(0);
    fetch(`${API_BASE}/questions?category=${encodeURIComponent(category)}`)
      .then(r => {
        if (!r.ok) throw new Error(`Failed to load ${category} (${r.status})`);
        return r.json();
      })
      .then(data => setQuestions(Array.isArray(data) ? data : []))
      .catch(err => setError(err.message || 'Failed to load'))
      .finally(() => setLoading(false));
  };

  const handleBack = () => {
    // small delay to allow shrink animation before unmount
    setIsAnimatingBack(true);
    setTimeout(() => {
      setSelectedCategory(null);
      setIsAnimatingBack(false);
    }, 220);
  };

  return (
    <div className="App">
      <div className="TriviaContainer">
        {!selectedCategory && (
          <div className="HeroCard">
            <h1 className="HeroTitle">Trivia Game</h1>
            <p className="HeroSubtitle">Select a category to begin:</p>
            <div className="ButtonsGrid minimal">
              {categories.map((cat) => (
                <button
                  key={cat}
                  className="TriviaButton minimal gradient"
                  onClick={() => handleSelect(cat)}
                >
                  {cat}
                </button>
              ))}
            </div>
            {error && (
              <p className="Hint" style={{ color: '#ff6b6b' }}>{error}</p>
            )}
          </div>
        )}

        {selectedCategory && (
          <div className={`SelectionStage ${isAnimatingBack ? 'shrinkOut' : 'growIn'}`}>
            <h1 className="CategoryTitle minimalTitle">{selectedCategory}</h1>
            {loading && (
              <div className="QuestionPlaceholder minimalCard">
                <p>Loading questions…</p>
              </div>
            )}
            {!loading && error && (
              <div className="QuestionPlaceholder minimalCard">
                <p style={{ color: '#ff6b6b' }}>{error}</p>
              </div>
            )}
            {!loading && !error && questions.length === 0 && (
              <div className="QuestionPlaceholder minimalCard">
                <p>No questions found for "{selectedCategory}".</p>
              </div>
            )}
            {!loading && !error && questions.length > 0 && (
              <div className="QuestionCard minimalCard">
                {(() => {
                  const q = questions[currentIndex] || {};
                  const questionText = q.text || q.question || '';
                  const rawAnswers = q.answers || q.Answers || [];
                  const answers = Array.isArray(rawAnswers) ? rawAnswers.slice(0, 4) : [];
                  const colors = [
                    { name: 'green', color: '#2ecc71' },
                    { name: 'blue', color: '#3498db' },
                    { name: 'red', color: '#e74c3c' },
                    { name: 'yellow', color: '#f1c40f' }
                  ];
                  return (
                    <>
                      <div className="QuestionText" style={{ marginBottom: 12, fontSize: 20, fontWeight: 600 }}>
                        {questionText || 'Untitled question'}
                      </div>
                      <div className="AnswersGrid">
                        {colors.map((c, idx) => (
                          <button
                            key={c.name}
                            className={`TriviaButton minimal btn-${c.name}`}
                            disabled={!answers[idx]}
                            onClick={() => { /* selection hook */ }}
                            aria-label={`Answer ${idx + 1}`}
                          >
                            {answers[idx] || '—'}
                          </button>
                        ))}
                      </div>
                      <div style={{ marginTop: 12, display: 'flex', justifyContent: 'space-between' }}>
                        <button
                          className="BackButton"
                          onClick={() => setCurrentIndex(i => Math.max(0, i - 1))}
                          disabled={currentIndex === 0}
                        >
                          Previous
                        </button>
                        <span className="Hint">{currentIndex + 1} / {questions.length}</span>
                        <button
                          className="BackButton"
                          onClick={() => setCurrentIndex(i => Math.min(questions.length - 1, i + 1))}
                          disabled={currentIndex >= questions.length - 1}
                        >
                          Next
                        </button>
                      </div>
                    </>
                  );
                })()}
              </div>
            )}
            <button className="BackButton" onClick={handleBack} aria-label="Back to categories">
              Back
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
