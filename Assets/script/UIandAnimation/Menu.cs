using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Lena Room")]
    [SerializeField] private Button _LenaRoom;
    [SerializeField] private Image _LenaRoomImage;

    [SerializeField] private GameObject _buttons;
    [SerializeField] private TMP_Text _pressToPlayText;
    [SerializeField] private Button _start;
    [SerializeField] private Button _setting;
    [SerializeField] private Button _exit;
    [SerializeField] private Image _logo;
    [SerializeField] private Transform _pointToMoveTransform;

    [SerializeField] private float _duration = 2f;
    [SerializeField] private Vector3 _scaleAnimation = new Vector3(1.5f, 1.5f, 1.5f);
    [SerializeField] private Vector3 _rotateAnimation = new Vector3(0f, 0f, 10f);
    [SerializeField] private Image _SilentLoadImage;

    [Header("RoomCamera")]
    [SerializeField] private Camera _MainCamera;
    [SerializeField] private Camera _BalerinCamera;

    private bool _menuActivated;

    private void Awake()
    {
        _LenaRoomImage.gameObject.SetActive(false);
        _LenaRoom.gameObject.SetActive(false);
        _MainCamera = GetComponent<Camera>();
        _buttons.SetActive(false);
        _logo.DOFade(0f, 0f);
        _start.GetComponent<Image>().DOFade(0f, 0f);
        _setting.GetComponent<Image>().DOFade(0f, 0f);
        _exit.GetComponent<Image>().DOFade(0f, 0f);
        ScaleAnimation(_pressToPlayText.gameObject);
        RotationAnimation(_pressToPlayText.gameObject);
        FadeAnimation(_pressToPlayText.gameObject);
    }

    private void Update()
    {
        if (_menuActivated)
            return;

        if (Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            _menuActivated = true;
            MenuOn();
        }
    }

    private void MenuOn()
    {
        _buttons.SetActive(true);
        _pressToPlayText.transform.DOKill();
        _pressToPlayText.DOKill();

        _pressToPlayText.DOFade(0f, 0.5f).OnComplete(() =>
        {
            this.transform.DOMove(_pointToMoveTransform.position, _duration)
            .OnComplete(() =>
            {
                _logo.DOFade(1f, 0.5f);
                _start.GetComponent<Image>().DOFade(1f, 1f);
                _LenaRoom.gameObject.SetActive(true);
                _LenaRoom.GetComponent<Image>().DOFade(1f, 1f);
                _setting.GetComponent<Image>().DOFade(1f, 1.5f);
                _exit.GetComponent<Image>().DOFade(1f, 2f);
            });
        });
    }

    public void GoToBalerin()
    {
        MoveSilentLoadImage(true, 1400f, 1f).OnComplete(() =>
        {
            _logo.gameObject.SetActive(false);
            _start.gameObject.SetActive(false);
            _LenaRoom.gameObject.SetActive(false);
            _setting.gameObject.SetActive(false);
            _exit.gameObject.SetActive(false);
            _LenaRoomImage.gameObject.SetActive(true);

            _BalerinCamera.enabled = true;
            _MainCamera.enabled = false;

            MoveSilentLoadImage(false, 1400f, 1f);
        });
    }

    public void GoMainMenu()
    {
        MoveSilentLoadImage(true, 1400f, 1f).OnComplete(() =>
        {
            _logo.gameObject.SetActive(true);
            _start.gameObject.SetActive(true);
            _LenaRoom.gameObject.SetActive(true);
            _setting.gameObject.SetActive(true);
            _exit.gameObject.SetActive(true);
            _LenaRoomImage.gameObject.SetActive(false);

            _BalerinCamera.enabled = false;
            _MainCamera.enabled = true;

            MoveSilentLoadImage(false, 1400f, 1f);
        });
    }

    public Tweener MoveSilentLoadImage(bool moveDown, float moveDistance = 1400f, float duration = 2f)
    {
        Vector3 targetPosition = _SilentLoadImage.rectTransform.position;
        targetPosition.y += moveDown ? -moveDistance : moveDistance;
        return _SilentLoadImage.rectTransform.DOMoveY(targetPosition.y, duration).SetEase(Ease.InOutSine);
    }

    private void ScaleAnimation(GameObject animatedObject)
    {
        animatedObject.transform.DOScale(_scaleAnimation, _duration).SetLoops(-1, LoopType.Yoyo);
    }

    private void RotationAnimation(GameObject animatedObject)
    {
        animatedObject.transform.DORotate(_rotateAnimation, _duration, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Yoyo);
    }

    private void FadeAnimation(GameObject animatedObject)
    {
        animatedObject.GetComponent<TMP_Text>().DOFade(0.3f, _duration).SetLoops(-1, LoopType.Yoyo);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }
}
