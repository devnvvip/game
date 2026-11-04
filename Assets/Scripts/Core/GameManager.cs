using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamerGame.Core
{
    public enum GameState
    {
        Walking,
        SittingAtDesk,
        PhoneOpen,
        Livestreaming,
        Paused,
        DaySummary
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState currentState = GameState.Walking;

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetGameState(GameState.Walking);
        }

        public void SetGameState(GameState newState)
        {
            currentState = newState;

            bool lockCursor = (currentState == GameState.Walking);
            #if UNITY_STANDALONE || UNITY_EDITOR
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
            #else
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            #endif

            Time.timeScale = (currentState == GameState.Paused) ? 0f : 1f;

            OnGameStateChanged?.Invoke(currentState);
        }

        public void PauseGame(bool pause)
        {
            if (pause)
            {
                SetGameState(GameState.Paused);
            }
            else
            {
                SetGameState(GameState.Walking);
            }
        }

        public void CompleteDay()
        {
            SetGameState(GameState.DaySummary);
        }

        public void RestartDay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
