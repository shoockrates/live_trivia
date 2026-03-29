import React, { useState } from "react";
import "./AddQuestionForm.css";

const categories = [
  "Science",
  "History",
  "Geography",
  "Sports",
  "Movies",
  "Music",
  "Literature",
  "Art",
  "Arts & Literature",
  "Technology",
  "Mathematics",
  "Biology",
  "Chemistry",
  "Physics",
  "Astronomy",
  "Politics",
  "Food",
  "Travel",
  "Nature",
  "Animals",
  "Space",
];

const getCategoryIcon = (category) => {
  const icons = {
    Science: "🔬",
    History: "📚",
    Geography: "🌍",
    Sports: "⚽",
    Movies: "🎬",
    Music: "🎵",
    Literature: "📖",
    Art: "🎨",
    "Arts & Literature": "🎭",
    Technology: "💻",
    Mathematics: "🔢",
    Biology: "🧬",
    Chemistry: "⚗️",
    Physics: "⚛️",
    Astronomy: "🌌",
    Politics: "🏛️",
    Food: "🍕",
    Travel: "✈️",
    Nature: "🌿",
    Animals: "🐾",
    Space: "🚀",
  };
  return icons[category] || "🧠";
};

const emptyQuestion = () => ({
  text: "",
  type: "single",
  answers: [
    { text: "", correct: false },
    { text: "", correct: false },
    { text: "", correct: false },
    { text: "", correct: false },
  ],
});

const AddQuizForm = ({ onBack, onSuccess }) => {
  const [quizName, setQuizName] = useState("");
  const [category, setCategory] = useState("");
  const [difficulty, setDifficulty] = useState("");
  const [questions, setQuestions] = useState([emptyQuestion()]);
  const [collapsed, setCollapsed] = useState({});
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [fadeOut, setFadeOut] = useState(false);

  const addQuestion = () => setQuestions((prev) => [...prev, emptyQuestion()]);

  const removeQuestion = (qi) => {
    setQuestions((prev) => prev.filter((_, i) => i !== qi));
    setCollapsed((prev) => {
      const next = { ...prev };
      delete next[qi];
      return next;
    });
  };

  const toggleCollapse = (qi) =>
    setCollapsed((prev) => ({ ...prev, [qi]: !prev[qi] }));

  const updateQuestion = (qi, field, value) =>
    setQuestions((prev) =>
      prev.map((q, i) => (i === qi ? { ...q, [field]: value } : q)),
    );

  const updateAnswer = (qi, ai, value) =>
    setQuestions((prev) =>
      prev.map((q, i) => {
        if (i !== qi) return q;
        const answers = [...q.answers];
        answers[ai] = { ...answers[ai], text: value };
        return { ...q, answers };
      }),
    );

  const toggleCorrect = (qi, ai) =>
    setQuestions((prev) =>
      prev.map((q, i) => {
        if (i !== qi) return q;
        const answers = q.answers.map((a, idx) => ({
          ...a,
          correct:
            q.type === "single"
              ? idx === ai
              : idx === ai
                ? !a.correct
                : a.correct,
        }));
        return { ...q, answers };
      }),
    );

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!quizName.trim()) return setError("Please enter a quiz name.");
    if (!category) return setError("Please select a category.");
    if (!difficulty) return setError("Please select a difficulty.");
    if (questions.length === 0) return setError("Add at least one question.");

    for (let i = 0; i < questions.length; i++) {
      const q = questions[i];
      if (!q.text.trim())
        return setError(`Question ${i + 1}: enter question text.`);
      if (q.answers.some((a) => !a.text.trim()))
        return setError(`Question ${i + 1}: fill all answer fields.`);
      if (!q.answers.some((a) => a.correct))
        return setError(`Question ${i + 1}: mark at least one correct answer.`);
    }

    const payload = {
      name: quizName,
      category,
      difficulty,
      questions: questions.map((q) => ({
        text: q.text,
        answers: q.answers.map((a) => a.text),
        correctAnswerIndexes: q.answers
          .map((a, i) => (a.correct ? i : null))
          .filter((i) => i !== null),
        category,
        difficulty,
      })),
    };

    try {
      const res = await fetch("http://localhost:5216/quizzes/submit-quiz", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw new Error("Failed to submit quiz.");

      setQuizName("");
      setCategory("");
      setDifficulty("");
      setQuestions([emptyQuestion()]);
      setCollapsed({});
      if (onSuccess) onSuccess();

      setSuccess("Quiz submitted successfully!");
      setFadeOut(false);
      setTimeout(() => setFadeOut(true), 2000);
      setTimeout(() => {
        setSuccess("");
        setFadeOut(false);
      }, 2500);
    } catch (err) {
      setError(err.message);
    }
    window.location.reload();
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2 className="auth-title">Create Quiz</h2>
        <p className="auth-subtitle">Fill in quiz details then add questions</p>

        {error && <div className="error-message">{error}</div>}
        {success && (
          <div className={`success-message ${fadeOut ? "fade-out" : ""}`}>
            {success}
          </div>
        )}

        <div className="input-group">
          <label className="selector-text">Quiz Name</label>
          <input
            type="text"
            placeholder="Enter quiz name"
            value={quizName}
            onChange={(e) => setQuizName(e.target.value)}
          />
        </div>

        <div className="input-group">
          <label className="selector-text">Category</label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            <option value="">-- Select Category --</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {getCategoryIcon(c)} {c}
              </option>
            ))}
          </select>
        </div>

        <div className="input-group">
          <label className="selector-text">Difficulty</label>
          <select
            value={difficulty}
            onChange={(e) => setDifficulty(e.target.value)}
          >
            <option value="">-- Select Difficulty --</option>
            <option value="easy">Easy</option>
            <option value="medium">Medium</option>
            <option value="hard">Hard</option>
          </select>
        </div>

        {/* Questions section */}
        <div className="input-group">
          <div className="quiz-questions-header">
            <label className="selector-text">
              Questions ({questions.length})
            </label>
            <button className="add-question-button" onClick={addQuestion}>
              + Add Question
            </button>
          </div>

          {questions.map((q, qi) => (
            <div key={qi} className="quiz-question-card">
              {/* Question header — click to collapse */}
              <div
                className="quiz-question-header"
                onClick={() => toggleCollapse(qi)}
              >
                <label className="selector-text quiz-question-label">
                  {collapsed[qi] ? "▶" : "▼"}&nbsp;Question {qi + 1}
                  {q.text && (
                    <span className="quiz-question-preview">
                      — {q.text.slice(0, 40)}
                      {q.text.length > 40 ? "…" : ""}
                    </span>
                  )}
                </label>
                {questions.length > 1 && (
                  <button
                    className="return-button quiz-remove-btn"
                    onClick={(e) => {
                      e.stopPropagation();
                      removeQuestion(qi);
                    }}
                  >
                    Remove
                  </button>
                )}
              </div>

              {/* Question body */}
              {!collapsed[qi] && (
                <>
                  <div className="input-group">
                    <label className="selector-text">Question Type</label>
                    <select
                      value={q.type}
                      onChange={(e) =>
                        updateQuestion(qi, "type", e.target.value)
                      }
                    >
                      <option value="single">Single Answer</option>
                      <option value="multiple">Multiple Answers</option>
                    </select>
                  </div>

                  <div className="input-group">
                    <label className="selector-text">Question Text</label>
                    <input
                      type="text"
                      placeholder="Enter question text"
                      value={q.text}
                      onChange={(e) =>
                        updateQuestion(qi, "text", e.target.value)
                      }
                    />
                  </div>

                  <div className="input-group">
                    <label className="selector-text">Answers</label>
                    {q.answers.map((a, ai) => (
                      <div key={ai} className="answer-row">
                        <input
                          type="text"
                          placeholder={`Answer ${ai + 1}`}
                          value={a.text}
                          onChange={(e) => updateAnswer(qi, ai, e.target.value)}
                        />
                        <label className="correct-label">
                          <input
                            type={q.type === "single" ? "radio" : "checkbox"}
                            name={`correct-${qi}`}
                            checked={a.correct}
                            onChange={() => toggleCorrect(qi, ai)}
                          />
                          Correct
                        </label>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="buttons-container">
          <button className="return-button" onClick={onBack}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <path d="M8 0L0 8l8 8V0z" />
            </svg>
            Back
          </button>
          <button className="add-question-button" onClick={handleSubmit}>
            Create Quiz
          </button>
        </div>
      </div>
    </div>
  );
};

export default AddQuizForm;
