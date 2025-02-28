using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] Sprite[] cardSprites;
    [SerializeField] int initialCards = 5;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    [SerializeField] int columns = 5; // Número de columnas en la cuadrícula
    [SerializeField] float spacing = 0.5f; // Espaciado entre cartas
    [SerializeField] float initialPauseTime = 2f;
    [SerializeField] float initialShowTime = 5f;
    [SerializeField] public AudioClip clickSound { get; private set;}
    [SerializeField] AudioClip matchSound;
    [SerializeField] AudioClip mismatchSound;
    [SerializeField] AudioClip introMusic;
    [SerializeField] AudioSource FXAudioSource;
    [SerializeField] AudioSource musicAudioSource;
    [SerializeField] GameObject UIStart;
    [SerializeField] GameObject UIVictory;
    [SerializeField] GameObject UIGetReady;
    [SerializeField] GameObject explosionPrefab;
    [HideInInspector] public bool playerCanClick = false;
    Sprite[] currentCards;
    List<Card> instantiatedCards = new List<Card>();
    Card firstFlippedCard = null;
    Card secondFlippedCard = null;
    int matchCount = 0;


    void Start()
    {
        PlayMusic(introMusic, 0.5f, true);
    }
    void CreateDeck()
    {
        List<Sprite> selectedCards = new List<Sprite>();

        // Seleccionar initialCards únicas
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
        yield return new WaitForSeconds(initialPauseTime); // Pequeña pausa inicial

        UIGetReady.SetActive(false); // Activar la UIGetReady
        // Mostrar todas las cartas (boca arriba)
        foreach (var card in instantiatedCards)
        {
            card.FlipCard(); // Girar para mostrar la parte frontal
        }

        yield return new WaitForSeconds(initialShowTime); // Esperar el tiempo de visualización

        // Volver a ponerlas boca abajo
        foreach (var card in instantiatedCards)
        {
            card.FlipCard(); // Girar nuevamente para esconder
        }

        playerCanClick = true; // Activar playerCanClick
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

        if (firstFlippedCard.frontSpriteRenderer.sprite.name == secondFlippedCard.frontSpriteRenderer.sprite.name)
        {
            PlaySoundFX(matchSound, 0.5f);
            Instantiate(explosionPrefab, firstFlippedCard.transform.position, Quaternion.identity);
            Destroy(firstFlippedCard.gameObject);
            Instantiate(explosionPrefab, secondFlippedCard.transform.position, Quaternion.identity);
            Destroy(secondFlippedCard.gameObject);
            matchCount++;

            if (matchCount == initialCards)
            {
                UIVictory.SetActive(true);
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
        FXAudioSource.Stop();
        FXAudioSource.volume = volume;
        FXAudioSource.clip = soundFX;
        FXAudioSource.Play();
    }
}