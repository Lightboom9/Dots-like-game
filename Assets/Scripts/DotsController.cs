using System;
using System.Collections;
using System.Collections.Generic;
using Dots;
using Dots.Misc;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Assets.Scripts
{
    public class DotsController : MonoBehaviour
    {
        public static DotsController Instance { get; private set; }

        private RectTransform _rectTransform;

        [Header("Game controls")]
        [SerializeField] private float _dotSquareSizeMult = 0.8f;
        [SerializeField] private int _gameWidthHeight = 6;
        [SerializeField] private GameObject _dotPrefab;
        [SerializeField] private GameObject _dotSelectAnimationPrefab;
        [SerializeField] private Transform _dotsConnectorParent;
        [SerializeField] private GameObject _dotsConnectorPrefab;
        [SerializeField] private TextMeshProUGUI _scoreTextbox;
        [SerializeField] private Button _menuButton;

        private float _connectionWidth;
        private List<DotInputHandler> _connectionChain = new List<DotInputHandler>();
        private List<GameObject> _connectionObjects = new List<GameObject>();

        private Vector2 _baseInstantiatePos;
        private float _dotSquareSide;
        private DotInputHandler[,] _dots;

        private DotsGame _dotsGame;

        [Space]
        [Header("Animation controls")]
        [SerializeField] private float _animationStartAdditionalMaxDelay = 0.75f;
        [SerializeField] private float _animationDotFallMaxDelay = 0.6f;
        [SerializeField] private float _animationDotFallTime = 0.5f;
        [SerializeField] private float _animationDotResizeStartSize = 0.25f;
        [SerializeField] private float _animationDotResizeMaxTime = 0.6f;
        [SerializeField] private float _animationDotMoveDefaultTime = 0.4f;
        [SerializeField] private float _animationDotMoveAdditionalTime = 0.2f;
        [SerializeField] private float _animationDotRemoveTime = 0.4f;

        private float _lockInputPause;
        private bool _lockInput;

        private bool _selectionIsFull;

        private List<DotInputHandler> _fallingDots = new List<DotInputHandler>();
        private List<Vector2Int> _fallingDotsFinalPositions = new List<Vector2Int>();

        private int _totalScore;

        private bool IsPointerPressed
        {
            get
            {
#if UNITY_EDITOR
                return Input.GetMouseButton(0);
#else
                return Input.touches.Length > 0;
#endif
            }
        }

        private Vector2 PointerPosition
        {
            get
            {
#if UNITY_EDITOR
                return Input.mousePosition;
#else

                if (Input.touches.Length == 0) return Vector2.zero;
                return Input.touches[0].position;
#endif
            }
        }

        private void Awake()
        {
            Instance = this;
            _rectTransform = GetComponent<RectTransform>();

            _menuButton.onClick.AddListener(OnMenuButtonClick);
        }

        private void Start()
        {
            _dotsGame = new DotsGame();

            _dotSquareSide = _rectTransform.sizeDelta.x / _gameWidthHeight;
            _connectionWidth = _dotSquareSide / 6f * _dotSquareSizeMult;
            _baseInstantiatePos = ((Vector2)_rectTransform.position) - new Vector2(_rectTransform.sizeDelta.x * _rectTransform.pivot.x, -1 * _rectTransform.sizeDelta.y * _rectTransform.pivot.y);
            _baseInstantiatePos += new Vector2(_dotSquareSide / 2f, _dotSquareSide / -2f);

            _dots = new DotInputHandler[_gameWidthHeight, _gameWidthHeight];

            _animationDotFallMaxDelay += _animationStartAdditionalMaxDelay;
            _dotsGame.OnDotSpawn += OnDotSpawnHandler;
            _dotsGame.OnDotMove += OnDotMoveHandler;
            _dotsGame.OnDotRemove += OnDotRemoveHandler;
            _dotsGame.Generate(_gameWidthHeight, _gameWidthHeight);
            _animationDotFallMaxDelay -= _animationStartAdditionalMaxDelay;
        }

        private void OnApplicationQuit()
        {
            SaveScore();
        }

        private void OnMenuButtonClick()
        {
            SaveScore();

            SceneManager.LoadScene(0);
        }

        private void SaveScore()
        {
            int bestScore = PlayerPrefs.GetInt("BestScore");
            if (_totalScore > bestScore) bestScore = _totalScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
        }

        private void OnDotSpawnHandler(Vector2Int pos)
        {
            GameObject go = Instantiate(_dotPrefab, transform);

            IEnumerator Animator()
            {
                GameObject dot = go.transform.GetChild(0).gameObject;
                RectTransform rt = go.GetComponent<RectTransform>();

                Vector2 startSize = new Vector2(_dotSquareSide * _animationDotResizeStartSize * _dotSquareSizeMult, _dotSquareSide * _animationDotResizeStartSize * _dotSquareSizeMult);
                Vector2 finalSize = new Vector2(_dotSquareSide * _dotSquareSizeMult, _dotSquareSide * _dotSquareSizeMult);
                rt.sizeDelta = startSize;

                DotInputHandler inputHandler = dot.GetComponent<DotInputHandler>();
                _dots[pos.x, pos.y] = inputHandler;
                _fallingDots.Add(inputHandler);
                _fallingDotsFinalPositions.Add(pos);

                switch (_dotsGame[pos.x, pos.y])
                {
                    case DotColor.Red:
                    {
                        dot.GetComponent<Image>().color = Color.red;

                        break;
                    }
                    case DotColor.Blue:
                    {
                        dot.GetComponent<Image>().color = Color.blue;

                        break;
                    }
                    case DotColor.Green:
                    {
                        dot.GetComponent<Image>().color = Color.green;

                        break;
                    }
                    case DotColor.Purple:
                    {
                        dot.GetComponent<Image>().color = new Color32(194, 71, 255, 255);

                        break;
                    }
                    case DotColor.Yellow:
                    {
                        dot.GetComponent<Image>().color = Color.yellow;

                        break;
                    }

                }

                go.transform.position = new Vector3(99999, 99999);

                // Wait for move and spawn calculations to be complete to know where exactly dot should actually fall (including possible moves after spawn)
                yield return new WaitForSeconds(0.2f);
                Vector2 finalPos = GetPositionForDot(_fallingDotsFinalPositions[_fallingDots.IndexOf(inputHandler)]);
                Vector2 spawnPos = finalPos + new Vector2(0, Screen.height);

                float timer = 0;
                float time = _animationDotFallTime;
                float resizeTime = Random.Range(_animationDotResizeMaxTime * 0.8f, _animationDotResizeMaxTime);
                float delay = Random.Range(0, _animationDotFallMaxDelay);
                _lockInputPause = Mathf.Max(_lockInputPause, time + delay + resizeTime);

                yield return new WaitForSeconds(delay);

                while (timer < time)
                {
                    if (go == null) yield break;

                    go.transform.position = Vector3.Lerp(spawnPos, finalPos, timer / _animationDotFallTime);

                    timer += Time.deltaTime;
                    yield return null;
                }

                go.transform.position = finalPos;

                timer = 0;
                while (timer < resizeTime)
                {
                    if (go == null) yield break;

                    rt.sizeDelta = Vector2.Lerp(startSize, finalSize, timer / time);

                    timer += Time.deltaTime;
                    yield return null;
                }

                if (_fallingDots.Contains(inputHandler))
                {
                    int index = _fallingDots.IndexOf(inputHandler);
                    _fallingDotsFinalPositions.RemoveAt(_fallingDots.IndexOf(inputHandler));
                    _fallingDots.Remove(inputHandler);
                }
                rt.sizeDelta = finalSize;
            }

            go.AddComponent<CoroutineRunner>().RunCoroutine(Animator());
        }

        private void OnDotMoveHandler(Vector2Int[] positions)
        {
            Vector2Int from = positions[0];
            Vector2Int to = positions[1];
            Transform tr = _dots[from.x, from.y].transform.parent;
            Vector2 newPos = GetPositionForDot(to);

            int fallingDotIndex = _fallingDots.IndexOf(_dots[from.x, from.y]);
            if (fallingDotIndex != -1)
            {
                _fallingDotsFinalPositions[fallingDotIndex] = to;
                _dots[to.x, to.y] = _dots[from.x, from.y];

                return;
            }

            _dots[to.x, to.y] = _dots[from.x, from.y];

            IEnumerator Animator()
            {
                Vector2 basePos = tr.position;

                float timer = 0;
                float time = _animationDotMoveDefaultTime + _animationDotMoveAdditionalTime * (to.y - from.y - 1);
                _lockInputPause = Mathf.Max(_lockInputPause, time);

                while (timer < time)
                {
                    tr.position = Vector2.Lerp(basePos, newPos, timer / time);

                    timer += Time.deltaTime;
                    yield return null;
                }

                tr.position = newPos;
            }

            tr.gameObject.AddComponent<CoroutineRunner>().RunCoroutine(Animator());
        }

        private void OnDotRemoveHandler(Vector2Int pos)
        {
            DotInputHandler inputHandler = _dots[pos.x, pos.y];
            RectTransform rt = inputHandler.transform.parent.GetComponent<RectTransform>();

            IEnumerator Animator()
            {
                Vector2 startSize = rt.sizeDelta;

                float timer = 0;
                float time = _animationDotRemoveTime;
                _lockInputPause = Mathf.Max(_lockInputPause, time);

                while (timer < time)
                {
                    rt.sizeDelta = Vector2.Lerp(startSize, Vector2.zero, timer / time);

                    timer += Time.deltaTime;
                    yield return null;
                }

                rt.sizeDelta = Vector2.zero;

                Destroy(rt.gameObject);
            }

            StartCoroutine(Animator());
        }

        private Vector2 GetPositionForDot(Vector2Int dotPos)
        {
            return new Vector2(_baseInstantiatePos.x + dotPos.x * _dotSquareSide, _baseInstantiatePos.y - dotPos.y * _dotSquareSide);
        }

        private Vector2Int GetCoords(DotInputHandler dot)
        {
            for (int i = 0; i < _gameWidthHeight; i++)
            {
                for (int j = 0; j < _gameWidthHeight; j++)
                {
                    if (_dots[i, j] == dot) return new Vector2Int(i, j);
                }
            }

            return new Vector2Int(-1, -1);
        }

        public void OnDotSelected(DotInputHandler dot)
        {
            if (_lockInput) return;

            if (_connectionChain.Count == 0)
            {
                AddDotToConnectionChain(dot);

                return;
            }
            if (_connectionChain.Count > 1 && _connectionChain[_connectionChain.Count - 1] == dot)
            {
                RemoveDotFromConnectionChain(dot);

                return;
            }

            Vector2Int oldPos = GetCoords(_connectionChain[_connectionChain.Count - 1]);
            Vector2Int newPos = GetCoords(dot);
            if (!_dotsGame.CanConnect(oldPos.x, oldPos.y, newPos.x, newPos.y)) return;

            if (_connectionChain.Count > 1 && _connectionChain.Contains(dot) && _connectionChain[_connectionChain.Count - 2] != dot && !_selectionIsFull)
            {
                _selectionIsFull = true;
                AddDotToConnectionChain(dot);

                return;
            }

            if (_selectionIsFull) return;
            AddDotToConnectionChain(dot);
        }

        private void UpdateScore()
        {
            int bonusScore = 0;
            for (int i = 1; i < _connectionChain.Count; i++) bonusScore += i;
            if (_selectionIsFull) bonusScore *= 2;

            _scoreTextbox.text = "Score: " + _totalScore;
            if (bonusScore > 0) _scoreTextbox.text += "+" + bonusScore;
        }

        private void AddDotToConnectionChain(DotInputHandler dot)
        {
            if (!IsPointerPressed) return;

            if (_connectionObjects.Count > 0)
            {
                // A workaround tbh, but I didn't want to rewrite the dots game class for this check
                if (_connectionObjects.Count > 1 && _connectionChain[_connectionChain.Count - 2] == dot) return;

                GameObject connector = _connectionObjects[_connectionChain.Count - 1];
                connector.name = "Connector " + Random.Range(0, 100000);

                connector.GetComponent<RectTransform>().sizeDelta = new Vector2(_connectionWidth, Vector3.Distance(connector.transform.position, dot.transform.position));
                connector.transform.rotation = Quaternion.Euler(0, 0, AngleBetweenTwoPoints(connector.transform.position, dot.transform.position) - 90f);
            }

            GameObject dotSelectAnimation = Instantiate(_dotSelectAnimationPrefab, dot.transform.position, Quaternion.identity, dot.transform);
            Image dotImage = dot.gameObject.GetComponent<Image>();
            Color selectAnimationColor = dotImage.color;
            selectAnimationColor.a = 0.25f;
            dotSelectAnimation.GetComponent<Image>().color = selectAnimationColor;
            dotSelectAnimation.GetComponent<DotSelectAnimationController>().Animate(new Vector2(_dotSquareSide, _dotSquareSide) / 2f * _dotSquareSizeMult,new Vector2(_dotSquareSide, _dotSquareSide) * 0.8f);

            _connectionChain.Add(dot);
            GameObject go = Instantiate(_dotsConnectorPrefab, dot.transform.position, Quaternion.identity, _dotsConnectorParent);
            go.GetComponent<Image>().color = dot.gameObject.GetComponent<Image>().color;
            go.name = "Connector " + Random.Range(0, 100000);
            _connectionObjects.Add(go);

            UpdateScore();
        }

        private void RemoveDotFromConnectionChain(DotInputHandler dot)
        {
            if (_connectionObjects.Count == 0) return;

            int lastIndex = _connectionChain.Count - 1;

            Destroy(_connectionObjects[lastIndex]);
            _connectionObjects.RemoveAt(lastIndex);
            _connectionChain.RemoveAt(lastIndex);

            if (_selectionIsFull) _selectionIsFull = false;

            UpdateScore();
        }

        private void OnPointerUp()
        {
            if (_connectionChain.Count > 1)
            {
                List<Vector2Int> includedDots = new List<Vector2Int>();
                foreach (var obj in _connectionChain)
                {
                    includedDots.Add(GetCoords(obj));
                }

                _totalScore += _dotsGame.ConnectPoints(includedDots.ToArray());
            }

            foreach (var obj in _connectionObjects)
            {
                Destroy(obj);
            }
            _connectionChain.Clear();
            _connectionObjects.Clear();

            _selectionIsFull = false;

            UpdateScore();
        }

        private void Update()
        {
            if (_lockInputPause > 0)
            {
                _lockInput = true;

                _lockInputPause -= Time.deltaTime;
            }
            else _lockInput = false;

            if (_connectionChain.Count == 0) return;

            if (_connectionChain.Count > 0)
            {
                GameObject connector = _connectionObjects[_connectionObjects.Count - 1];

                Vector2 pointerPosition = PointerPosition;
                connector.GetComponent<RectTransform>().sizeDelta = new Vector2(_connectionWidth, Vector3.Distance(connector.transform.position, pointerPosition));
                connector.transform.rotation = Quaternion.Euler(0, 0, AngleBetweenTwoPoints(connector.transform.position, pointerPosition) - 90f);
            }

            if (_connectionChain.Count > 0 && !IsPointerPressed)
            {
                OnPointerUp();
            }
        }

        private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }
    }
}