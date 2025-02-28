using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] Sprite[] cardSprites;
    [SerializeField] int initialCards = 5;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    [SerializeField] int columns = 5; // N�mero de columnas en la cuadr�cula
    [SerializeField] float spacing = 0.5f; // Espaciado entre cartas
    [SerializeField] float initialPauseTime = 2f;
    [SerializeField] float initialShowTime = 5f;
    [SerializeField] public AudioClip clickSound;
    [SerializeField] AudioClip matchSound;
    [SerializeField] AudioClip mismatchSound;
    [SerializeField] AudioClip introMusic;
    [SerializeField] AudioClip sceneMusic;
    [SerializeField] AudioClip victoryMusic;
    [SerializeField] AudioSource FXAudioSource;
    [SerializeField] AudioSource musicAudioSource;
    [SerializeField] GameObject UIStart;
    [SerializeField] GameObject UIVictory;
    [SerializeField] Text timeResultText;
    [SerializeField] GameObject UIGetReady;
    [SerializeField] GameObject UITimer;
    [SerializeField] Text timerText;
    [SerializeField] GameObject explosionPrefab;
    [HideInInspector] public bool playerCanClick = false;
    Sprite[] currentCards;
    List<Card> instantiatedCards = new List<Card>();
    Card firstFlippedCard = null;
    Card secondFlippedCard = null;
    int matchCount = 0;
    float elapsedTime = 0f;
    bool isTimeRunning = false;


    void Start()
    {
        UIVictory.SetActive(false);
        UIGetReady.SetActive(false);
        UITimer.SetActive(false);
        if (SceneManager.GetActiveScene().buildIndex == 0) { UIStart.SetActive(true); PlayMusic(introMusic, 0.5f, true); }// Reproducir la m�sica de introducci�n y activar UIStart si es el primer nivel de juego
        else { UIStart.SetActive(true); StartGame(); }       
    }
    private void Update()
    {
        if (isTimeRunning) // Si el tiempo est� corriendo, actualiza el temporizador
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText(elapsedTime);
        }
    }
    public void StartTimer()
    {
        elapsedTime = 0f;
        isTimeRunning = true;
    }

    public void StopTimer()
    {
        isTimeRunning = false;
    }
    private void UpdateTimerText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }
    private void UpdateTimeResultText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timeResultText.text = "YOUR TIME: " + string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    void CreateDeck()
    {
        List<Sprite> selectedCards = new List<Sprite>();

        // Seleccionar initialCards �nicas
        for (int i = 0; i < initialCards; i++)
        {
            selectedCards.Add(cardSprites[i]);
        }

        // Duplicar cada carta
        List<Sprite> deck = new List<Sprite>(selectedCards);
        deck.AddRange(selectedCards);

        // Mezclar el mazo
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (deck[k], deck[n]) = (deck[n], deck[k]); // Swap
        }

        // Convertir a array
        currentCards = deck.ToArray();

        InstantiateDeck();
    }

    void InstantiateDeck()
    {
        int totalRows = Mathf.CeilToInt((float)currentCards.Length / columns);
        int totalCols = Mathf.Min(columns, currentCards.Length);

        float offsetX = (totalCols - 1) * spacing * 0.5f;
        float offsetY = (totalRows - 1) * spacing * 0.5f;

        for (int i = 0; i < currentCards.Length; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, cardParent);
            Card cardComponent = newCard.GetComponent<Card>();
            cardComponent.frontSpriteRenderer.sprite = currentCards[i];

            // Posicionamiento centrado
            int row = i / columns;
            int col = i % columns;
            newCard.transform.localPosition = new Vector3(col * spacing - offsetX, -row * spacing + offsetY, 0);

            instantiatedCards.Add(cardComponent);
        }
    }

    IEnumerator ShowCardsTemporarily()
    {
        StopMusic();
        UIGetReady.SetActive(true); // Activar la UIGetReady
        yield return new WaitForSeconds(initialPauseTime); // Peque�a pausa inicial

        UIGetReady.SetActive(false); // Activar la UIGetReady
        // Mostrar todas las cartas (boca arriba)
        foreach (var card in instantiatedCards)
        {
            card.FlipCard(); // Girar para mostrar la parte frontal
        }

        yield return new WaitForSeconds(initialShowTime); // Esperar el tiempo de visualizaci�n

        // Volver a ponerlas boca abajo
        foreach (var card in instantiatedCards)
        {
            card.FlipCard(); // Girar nuevamente para esconder
        }

        playerCanClick = true; // Activar playerCanClick
        UITimer.SetActive(true); // Mostrar el temporizador
        StartTimer(); // Iniciar el temporizador
        PlayMusic(sceneMusic, 0.5f, true);
    }

    public void StartGame()
    {
        currentCards = new Sprite[initialCards * 2];
        CreateDeck();
        StartCoroutine(ShowCardsTemporarily());
        UIStart.SetActive(false);
        matchCount = 0;
    }

    public void CardFlipped(Card card)
    {
        if (firstFlippedCard == null)
        {
            firstFlippedCard = card;
        }
        else if (secondFlippedCard == null)
        {
            secondFlippedCard = card;
            playerCanClick = false;
            StartCoroutine(CheckMatch());
        }
    }

    IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(1f);

        if (firstFlippedCard.frontSpriteRenderer.sprite.name == secondFlippedCard.frontSpriteRenderer.sprite.name && firstFlippedCard != secondFlippedCard)
        {
            PlaySoundFX(matchSound, 0.5f);
            Instantiate(explosionPrefab, firstFlippedCard.transform.position, Quaternion.identity);
            Destroy(firstFlippedCard.gameObject);
            Instantiate(explosionPrefab, secondFlippedCard.transform.position, Quaternion.identity);
            Destroy(secondFlippedCard.gameObject);
            matchCount++;

            if (matchCount == initialCards)
            {
                StopMusic();
                PlayMusic(victoryMusic, 0.5f, false);
                UITimer.SetActive(false);
                StopTimer();
                UIVictory.SetActive(true);
                UpdateTimeResultText(elapsedTime);
            }
        }
        else
        {
            PlaySoundFX(mismatchSound, 0.8f);
            firstFlippedCard.FlipCard();
            secondFlippedCard.FlipCard();
        }

        firstFlippedCard = null;
        secondFlippedCard = null;
        playerCanClick = true;
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    void PlayMusic(AudioClip musicClip, float volume, bool playLoop)
    {
        musicAudioSource.Stop();
        musicAudioSource.volume = volume;
        musicAudioSource.clip = musicClip;
        musicAudioSource.loop = playLoop;
        musicAudioSource.Play();
    }
    void StopMusic()
    {
        musicAudioSource.Stop();
    }

    public void PlaySoundFX(AudioClip soundFX, float volume)
    {
        FXAudioSource.volume = volume;
        FXAudioSource.PlayOneShot(soundFX);
    }
}