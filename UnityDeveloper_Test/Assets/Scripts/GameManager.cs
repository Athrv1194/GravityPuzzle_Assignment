using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI winText;

    [Header("Game Settings")]
    public float timeLimit = 120f; // 2 Minutes
    private bool gameEnded = false;
    private int totalCubes;
    private int cubesCollected = 0;

    void Start()
    {
        // Automatically find how many "Collectible" objects exist
        totalCubes = GameObject.FindGameObjectsWithTag("Collectible").Length;
        winText.gameObject.SetActive(false); // Hide text at start
    }

    void Update()
    {
        if (gameEnded)
        {
            // Restart if R is pressed
            if (Input.GetKeyDown(KeyCode.R)) 
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Timer Logic
        if (timeLimit > 0)
        {
            timeLimit -= Time.deltaTime;
            
            // Format time like 01:45
            int minutes = Mathf.FloorToInt(timeLimit / 60);
            int seconds = Mathf.FloorToInt(timeLimit % 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            EndGame(false); // Time ran out!
        }
    }

    public void CubeCollected()
    {
        cubesCollected++;
        // Check if we found them all
        if (cubesCollected >= totalCubes)
        {
            EndGame(true); // Victory!
        }
    }

    // Called by PlayerController when the player has been falling too long
    public void PlayerFell()
    {
        if (gameEnded) return;
        EndGame(false);
    }

    void EndGame(bool win)
    {
        gameEnded = true;
        winText.gameObject.SetActive(true);
        
        if (win)
        {
            winText.text = "YOU WIN!\nPress R to Restart";
            winText.color = Color.green;
        }
        else
        {
            winText.text = "GAME OVER\nPress R to Restart";
            winText.color = Color.red;
        }
    }
}
