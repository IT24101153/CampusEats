import { useEffect, useState } from 'react'
import './App.css'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5100'

function App() {
  const [meals, setMeals] = useState([])
  const [message, setMessage] = useState('Loading meals...')
  const [form, setForm] = useState({ name: '', description: '', price: '' })

  async function loadMeals() {
    setMessage('Loading meals...')

    try {
      const response = await fetch(`${API_URL}/api/menu`)
      if (!response.ok) {
        const error = await response.json().catch(() => null)
        throw new Error(error?.message || error?.error || 'The API could not load the meals.')
      }

      const data = await response.json()
      setMeals(data)
      setMessage(data.length === 0 ? 'No meals found. Seed the sample data.' : '')
    } catch (error) {
      setMessage(error.message)
    }
  }

  async function createMeal(event) {
    event.preventDefault()
    setMessage('Adding meal...')

    try {
      const response = await fetch(`${API_URL}/api/menu`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name,
          description: form.description,
          price: Number(form.price),
        }),
      })
      if (!response.ok) {
        const error = await response.json().catch(() => null)
        throw new Error(error?.message || error?.error || 'Could not add the meal.')
      }

      setForm({ name: '', description: '', price: '' })
      await loadMeals()
    } catch (error) {
      setMessage(error.message)
    }
  }

  useEffect(() => {
    loadMeals()
  }, [])

  return (
    <main>
      <header>
        <h1>CampusEats</h1>
        <p>Simple food ordering for campus.</p>
      </header>

      <section>
        <h2>Menu</h2>
        {message && <p>{message}</p>}
        {meals.length > 0 && (
          <ul>
            {meals.map((meal) => (
              <li key={meal.id}>
                <strong>{meal.name}</strong> — {meal.description} — Rs. {meal.price}
              </li>
            ))}
          </ul>
        )}
        <h2>Add a meal</h2>
        <form onSubmit={createMeal}>
          <p>
            <label>
              Name<br />
              <input
                required
                value={form.name}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
              />
            </label>
          </p>
          <p>
            <label>
              Description<br />
              <input
                required
                value={form.description}
                onChange={(event) => setForm({ ...form, description: event.target.value })}
              />
            </label>
          </p>
          <p>
            <label>
              Price<br />
              <input
                required
                min="0.01"
                step="0.01"
                type="number"
                value={form.price}
                onChange={(event) => setForm({ ...form, price: event.target.value })}
              />
            </label>
          </p>
          <button type="submit">Create meal</button>
        </form>
        <button type="button" onClick={loadMeals}>Refresh meals</button>
      </section>
    </main>
  )
}

export default App
