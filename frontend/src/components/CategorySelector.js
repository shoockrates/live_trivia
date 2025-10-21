import React, { useState, useEffect } from 'react';
import './CategorySelector.css';

const CategorySelector = ({ categories, onSelectCategory, loading, error, user }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filteredCategories, setFilteredCategories] = useState(categories);
  const [selectedCategory, setSelectedCategory] = useState(null);

  useEffect(() => {
    if (searchTerm.trim() === '') {
      setFilteredCategories(categories);
    } else {
      setFilteredCategories(
        categories.filter(cat => 
          cat.toLowerCase().includes(searchTerm.toLowerCase())
        )
      );
    }
  }, [searchTerm, categories]);

  const handleCategoryClick = (category) => {
    setSelectedCategory(category);
    setTimeout(() => {
      onSelectCategory(category);
    }, 200);
  };

  const getCategoryIcon = (category) => {
    const icons = {
      'Science': '🔬',
      'History': '📚',
      'Geography': '🌍',
      'Sports': '⚽',
      'Movies': '🎬',
      'Music': '🎵',
      'Literature': '📖',
      'Art': '🎨',
      'Technology': '💻',
      'Mathematics': '🔢',
      'Biology': '🧬',
      'Chemistry': '⚗️',
      'Physics': '⚛️',
      'Astronomy': '🌌',
      'Politics': '🏛️',
      'Food': '🍕',
      'Travel': '✈️',
      'Nature': '🌿',
      'Animals': '🐾',
      'Space': '🚀'
    };
    return icons[category] || '🧠';
  };

  const getCategoryColor = (category) => {
    const colors = {
      'Science': '#e74c3c',
      'History': '#f39c12',
      'Geography': '#2ecc71',
      'Sports': '#3498db',
      'Movies': '#9b59b6',
      'Music': '#e91e63',
      'Literature': '#795548',
      'Art': '#ff9800',
      'Technology': '#607d8b',
      'Mathematics': '#3f51b5',
      'Biology': '#4caf50',
      'Chemistry': '#ff5722',
      'Physics': '#9c27b0',
      'Astronomy': '#673ab7',
      'Politics': '#f44336',
      'Food': '#ff9800',
      'Travel': '#00bcd4',
      'Nature': '#8bc34a',
      'Animals': '#ffc107',
      'Space': '#2196f3'
    };
    return colors[category] || '#6e7bff';
  };

  return (
    <div className="category-selector-container">
      <div className="category-selector-card">
        <div className="selector-header">
          <h1 className="selector-title">Choose Your Challenge</h1>
          <p className="selector-subtitle">Select a category to test your knowledge</p>
          
          <div className="search-container">
            <div className="search-input-wrapper">
              <svg className="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
              </svg>
              <input
                type="text"
                placeholder="Search categories..."
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
              <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
            </svg>
            {error}
          </div>
        )}

        {loading && (
          <div className="loading-container">
            <div className="loading-spinner"></div>
            <p>Loading categories...</p>
          </div>
        )}

        {!loading && !error && (
          <div className="categories-grid">
            {filteredCategories.map((category, index) => (
              <button
                key={category}
                className={`category-card ${selectedCategory === category ? 'selected' : ''}`}
                onClick={() => handleCategoryClick(category)}
                style={{
                  '--category-color': getCategoryColor(category),
                  animationDelay: `${index * 50}ms`
                }}
              >
                <div className="category-icon">
                  {getCategoryIcon(category)}
                </div>
                <div className="category-content">
                  <h3 className="category-name">{category}</h3>
                  <p className="category-description">
                    Test your knowledge in {category.toLowerCase()}
                  </p>
                </div>
                <div className="category-arrow">
                  <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <path d="M8 0L0 8l8 8V0z"/>
                  </svg>
                </div>
              </button>
            ))}
          </div>
        )}

        {!loading && !error && filteredCategories.length === 0 && searchTerm && (
          <div className="no-results">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="currentColor">
              <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 14z"/>
            </svg>
            <h3>No categories found</h3>
            <p>Try searching for something else</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default CategorySelector;
