using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;
using Rewired;
using System;
using System.Collections.Generic;
using System.Collections;

[Serializable]
public class UserInterfaceController
{
    PlayerInput gamePad;
    Canvas canvas;
    RectTransform MessageBox;
    [SerializeField]
    Button[] buttons;
    TextMeshProUGUI message;
    [SerializeField]
    double timer = 0;
   
   [SerializeField]
    double messageDuration;
    string[] tags = new string[4] { "Confirm: (X)", "Reject: (B)","Confirm: (Y)","Confirm: (A)"};
    public enum MessageType {Inactive, Inital, Followup}
    private MessageType _currentMessage;
    public UnityAction<MessageType> OnMessageChange;
    public MessageType CurrentMessage { get { return _currentMessage; } set { _currentMessage = value; OnMessageChange(_currentMessage); } }
    List<Action> NextMessage = new List<Action>();
    private bool _isHitConfirmPause;
    private PlayerEvents playerEvents;
    private PlayerStatBarUI _statBarUI = new PlayerStatBarUI();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    public void OnUpdate()
    {
        if (canvas.gameObject.activeSelf)
        {
            // GetUIButtonDown only passes through while Context == Dialog, so these
            // face-button checks cannot cross-fire into combat systems.
            if (gamePad.GetUIButtonDown("X") & buttons[0].gameObject.activeSelf) buttons[0].onClick.Invoke();
            if (gamePad.GetUIButtonDown("B") & buttons[1].gameObject.activeSelf) buttons[1].onClick.Invoke();
            if (gamePad.GetUIButtonDown("Y") & buttons[2].gameObject.activeSelf) buttons[2].onClick.Invoke();
            if (gamePad.GetUIButtonDown("A") & buttons[3].gameObject.activeSelf) buttons[3].onClick.Invoke();
        }

        //Debug.Log($"{canvas.gameObject.name} is {canvas.gameObject.activeSelf}");

        if (canvas.gameObject.activeSelf == true) { if ((timer += Time.deltaTime / messageDuration) >= 1) Clear();  }
        
        
    }

    public void Initialize(Camera camera, Canvas canvas, PlayerInput gamePad, PlayerEvents playerEvents)
    {
        this.gamePad = gamePad;
        this.canvas = canvas;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.sortingOrder = 10;
        canvas.planeDistance = 1;
        buttons = new Button[4];
        MessageBox = canvas.transform.Find("MessageBox").GetComponent<RectTransform>();

        // ── MessageBox: centred on screen, sized to hold message + full cross ─
        // The cross occupies ±(ARM_V + BTN_H/2) = ±68 px vertically.
        // The message sits ARM_V + BTN_H/2 + gap (15) + half-message-height above centre.
        // Total needed: ~128 px top + ~83 px bottom → 280 px is comfortable.
        MessageBox.anchorMin        = new Vector2(0.5f, 0.5f);
        MessageBox.anchorMax        = new Vector2(0.5f, 0.5f);
        MessageBox.pivot            = new Vector2(0.5f, 0.5f);
        MessageBox.anchoredPosition = Vector2.zero;
        MessageBox.sizeDelta        = new Vector2(370f, 280f);
        // Keep the prefab's local scale (1.5) — intentional design size.

        // ── Message container — the coloured panel above the button cross ─────
        // BUG FIX: previously the code moved the TMP *child* rect instead of the
        // container itself, leaving the background panel and the text 100 px apart.
        // Now we move the container (which holds both the Image background and
        // the TMP child) and stretch the TMP to fill it completely.
        RectTransform messageContainerRT = MessageBox.transform.Find("Message").GetComponent<RectTransform>();
        messageContainerRT.anchorMin        = new Vector2(0.5f, 0.5f);
        messageContainerRT.anchorMax        = new Vector2(0.5f, 0.5f);
        messageContainerRT.pivot            = new Vector2(0.5f, 0.5f);
        // Y=108: top button top-edge is ARM_V+BTN_H/2=68, gap=15, half-height=25 → 108
        messageContainerRT.anchoredPosition = new Vector2(0f, 108f);
        messageContainerRT.sizeDelta        = new Vector2(330f, 60f);

        // Stretch the TMP to fill the container so it always matches its background.
        message = messageContainerRT.GetComponentInChildren<TextMeshProUGUI>();
        RectTransform msgTmpRT  = message.GetComponent<RectTransform>();
        msgTmpRT.anchorMin        = Vector2.zero;
        msgTmpRT.anchorMax        = Vector2.one;
        msgTmpRT.anchoredPosition = Vector2.zero;
        msgTmpRT.sizeDelta        = Vector2.zero;

        message.enableAutoSizing   = true;
        message.fontSizeMin        = 12f;
        message.fontSizeMax        = 22f;
        message.alignment          = TextAlignmentOptions.Center;
        message.enableWordWrapping = true;

        // ── Find buttons by name — safer than tag search in multiplayer ───────
        // Tag search (FindGameObjectWithTag) finds the first match across ALL active
        // objects in the scene; in a 4-player game it would find another player's
        // buttons. Name search scoped to MessageBox is always the correct canvas.
        buttons[0] = MessageBox.transform.Find("Confirm: (X)").GetComponent<Button>();
        buttons[1] = MessageBox.transform.Find("Reject: (B)").GetComponent<Button>();
        buttons[2] = MessageBox.transform.Find("Confirm: (Y)").GetComponent<Button>();
        buttons[3] = MessageBox.transform.Find("Confirm: (A)").GetComponent<Button>();

        // ── Default button layout (repositioned each SetMessage call) ─────────
        LayoutButtons(2);

        //canvas = new GameObject("Canvas",typeof(RectTransform)).AddComponent<Canvas>();
        //canvas.AddComponent<CanvasScaler>();
        //canvas.AddComponent<GraphicRaycaster>();
        //canvas.GetComponent<RectTransform>().sizeDelta = new Vector3(1, 1, 1);
        //canvas.renderMode = RenderMode.ScreenSpaceCamera;
        //canvas.worldCamera = myCamera;

        //MessageBox = new GameObject("Message Box", typeof(RectTransform)).GetComponent<RectTransform>();
        //MessageBox.transform.SetParent(canvas.transform);
        //buttons = new Button[2];
        //for (int i = 0; i < buttons.Length; i++)
        //{
        //    GameObject button = (new GameObject(((i == 0) ? "Confirm" : "Reject"), typeof(RectTransform)));
        //    button.transform.SetParent(MessageBox); 
        //    RectTransform buttonRect = button.GetComponent<RectTransform>();
        //    buttonRect.sizeDelta = new Vector3(1, 1, 1);

        //    buttonRect.anchoredPosition = new Vector2(90, 20) * ((i == 0) ? -1 : 1);
        //    button.AddComponent<Button>();
        //    button.AddComponent<Image>();

        //    GameObject text = new GameObject("Text (TMP>", typeof(RectTransform));

        //    RectTransform textRect = text.GetComponent<RectTransform>();
        //    textRect.anchoredPosition = Vector2.zero;
        //    textRect.sizeDelta = new Vector3(1, 1, 1);
        //    TextMeshProUGUI textMesh = text.AddComponent<TextMeshProUGUI>();
        //    textMesh.text = string.Empty;
        //    textMesh.alignment = TextAlignmentOptions.Center;

        //    textRect.SetParent(buttonRect);

        //    buttons[i] = button.GetComponent<Button>();

        //}

        //GameObject messageObject = new GameObject("Message", typeof(RectTransform));
        //messageObject.transform.SetParent(MessageBox);
        //RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        //messageRect.sizeDelta = new Vector3(1, 1, 1);
        //messageRect.anchoredPosition = new Vector2(0, 20);
        //messageRect.sizeDelta = new Vector2(335, 50);
        //TextMeshProUGUI messageMesh = messageObject.AddComponent<TextMeshProUGUI>();
        //messageMesh.text = "Message";
        //messageMesh.alignment = TextAlignmentOptions.Center;
        //this.message = messageMesh;


        //message = messageMesh;

        buttons[1].onClick.AddListener(Clear);
        canvas.gameObject.SetActive(false);

        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate += OnUpdate;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;

        _statBarUI.Initialize(camera, playerEvents);
    }
    public void Deactivate()
    {
        _statBarUI.Deactivate();

        this.playerEvents.OnUpdate -= OnUpdate;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents = null;
        GameObject.Destroy(canvas.gameObject);
         canvas = null;
        MessageBox = null;       
        buttons = null;
        message = null;

    }
    public void SetOwner(LocalPlayerManager owner)
    {
        _statBarUI.SetOwner(owner);
    }

    public void SetCanvasName(string playerName)
    {
        if (canvas != null)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(playerName);
            stringBuilder.Append(" Canvas");
            canvas.name = stringBuilder.ToString();
        }

        _statBarUI.SetPlayerName(playerName);
    }

    //public void SetMessage(Message message)

    //{

    //}

    public void SetMessage(string message, UnityAction confirmX, string confirmXButtonText, double messageDuration)
    {
        if (!canvas.gameObject.activeSelf)
        {
            LayoutButtons(1);
            canvas.gameObject.SetActive(true);
            playerEvents?.OnDialogStateChanged?.Invoke(true);
            buttons[0].gameObject.SetActive(true);
            buttons[1].gameObject.SetActive(false);
            buttons[2].gameObject.SetActive(false);
            buttons[3].gameObject.SetActive(false);

            this.message.text = message;

            buttons[0].onClick.AddListener(confirmX);
            buttons[0].onClick.AddListener(Clear);
            buttons[1].onClick.AddListener(Clear);


            this.messageDuration = messageDuration;

            StringBuilder buttonText = new StringBuilder();
            buttonText.Append(tags[0]);
            buttonText.Replace("Confirm", confirmXButtonText);
            buttons[0].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = buttonText.ToString();


        }
        else
        {
            Debug.LogWarning($"Cannot send message when current Session is active");


            Action action = () =>
            {
                canvas.gameObject.SetActive(true);
                playerEvents?.OnDialogStateChanged?.Invoke(true);
                buttons[0].gameObject.SetActive(true);
                buttons[1].gameObject.SetActive(true);
                buttons[2].gameObject.SetActive(false);
                buttons[3].gameObject.SetActive(false);

                this.message.text = message;

                buttons[0].onClick.AddListener(confirmX);
                buttons[0].onClick.AddListener(Clear);
                buttons[1].onClick.AddListener(Clear);


                this.messageDuration = messageDuration;

                StringBuilder buttonText = new StringBuilder();
                buttonText.Append(tags[0]);
                buttonText.Replace("Confirm", confirmXButtonText);
                buttons[0].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = buttonText.ToString();
            };

            NextMessage.Add(action);
        }

    }

    //public IEnumerator NextMessage(string message, UnityAction confirmX, string confirmXButtonText, double messageDuration)
    //{
        
       
    //}
    public void SetMessage(string message, UnityAction confirmX, UnityAction rejectB, (string confirmX, string rejectB) buttonText, double messageDuration)
    {
        if (!canvas.gameObject.activeSelf)
        {
            LayoutButtons(2);
            canvas.gameObject.SetActive(true);
            playerEvents?.OnDialogStateChanged?.Invoke(true);
            buttons[0].gameObject.SetActive(true);
            buttons[1].gameObject.SetActive(true);
            buttons[2].gameObject.SetActive(false);
            buttons[3].gameObject.SetActive(false);


            this.message.text = message;


            buttons[0].onClick.AddListener(confirmX);
            buttons[0].onClick.AddListener(Clear);
            buttons[1].onClick.AddListener(rejectB);
            buttons[1].onClick.AddListener(Clear);  
            
            
            
            this.messageDuration = messageDuration;

            StringBuilder sbButtonText = new StringBuilder();
            sbButtonText.Append(tags[0]);
            sbButtonText.Replace("Confirm", buttonText.confirmX);
            buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append(tags[1]);
            sbButtonText.Replace("Reject", buttonText.rejectB);
            buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            
        }
        else
        {
            Debug.LogWarning($"Cannot send message when current Session is active");
            Action action = () => {
                canvas.gameObject.SetActive(true);
                playerEvents?.OnDialogStateChanged?.Invoke(true);
                buttons[0].gameObject.SetActive(true);
                buttons[1].gameObject.SetActive(true);
                buttons[2].gameObject.SetActive(false);
                buttons[3].gameObject.SetActive(false);


                this.message.text = message;


                buttons[0].onClick.AddListener(confirmX);
                buttons[0].onClick.AddListener(Clear);
                buttons[1].onClick.AddListener(rejectB);
                buttons[1].onClick.AddListener(Clear);



                this.messageDuration = messageDuration;

                StringBuilder sbButtonText = new StringBuilder();
                sbButtonText.Append(tags[0]);
                sbButtonText.Replace("Confirm", buttonText.confirmX);
                buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append(tags[1]);
                sbButtonText.Replace("Reject", buttonText.rejectB);
                buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            };
            NextMessage.Add(action);
        }

    }

    public void SetMessage(string message, UnityAction confirmX,  UnityAction confirmY, UnityAction rejectB,(string  confirmX, string confirmY, string rejectB) buttonText, double messageDuration)
    {
        if (!canvas.gameObject.activeSelf)
        {
            LayoutButtons(3);
            canvas.gameObject.SetActive(true);
            playerEvents?.OnDialogStateChanged?.Invoke(true);
            buttons[0].gameObject.SetActive(true);
            buttons[1].gameObject.SetActive(true);
            buttons[2].gameObject.SetActive(true);
            buttons[3].gameObject.SetActive(false);


            this.message.text = message;
            buttons[0].onClick.AddListener(confirmX);
            buttons[0].onClick.AddListener(Clear);
            buttons[1].onClick.AddListener(rejectB);
            buttons[1].onClick.AddListener(Clear);
            buttons[2].onClick.AddListener(confirmY);
            buttons[2].onClick.AddListener(Clear);


            this.messageDuration = messageDuration;

            StringBuilder sbButtonText = new StringBuilder();
            sbButtonText.Append("Confirm: (X)");
            sbButtonText.Replace("Confirm", buttonText.confirmX);
            buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append("Reject: (B)");
            sbButtonText.Replace("Reject", buttonText.rejectB);
            buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append("Confirm: (Y)");
            sbButtonText.Replace("Confirm", buttonText.confirmY);
            buttons[2].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();
        }



        else
        {
            Debug.LogWarning($"Cannot send message when current Session is active");
            Action action = () => {
                canvas.gameObject.SetActive(true);
                playerEvents?.OnDialogStateChanged?.Invoke(true);
                buttons[0].gameObject.SetActive(true);
                buttons[1].gameObject.SetActive(true);
                buttons[2].gameObject.SetActive(true);
                buttons[3].gameObject.SetActive(false);


                this.message.text = message;
                buttons[0].onClick.AddListener(confirmX);
                buttons[0].onClick.AddListener(Clear);
                buttons[1].onClick.AddListener(rejectB);
                buttons[1].onClick.AddListener(Clear);
                buttons[2].onClick.AddListener(confirmY);
                buttons[2].onClick.AddListener(Clear);


                this.messageDuration = messageDuration;

                StringBuilder sbButtonText = new StringBuilder();
                sbButtonText.Append("Confirm: (X)");
                sbButtonText.Replace("Confirm", buttonText.confirmX);
                buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append("Reject: (B)");
                sbButtonText.Replace("Reject", buttonText.rejectB);
                buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append("Confirm: (Y)");
                sbButtonText.Replace("Confirm", buttonText.confirmY);
                buttons[2].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();
            };
            NextMessage.Add(action);
        }

    }
    public void SetMessage(string message, UnityAction confirmX, UnityAction confirmY, UnityAction confirmA, UnityAction rejectB, (string confirmX, string confirmY, string confirmA, string rejectB) buttonText, double messageDuration)
    {
        if (!canvas.gameObject.activeSelf)
        {
            LayoutButtons(4);
            canvas.gameObject.SetActive(true);
            playerEvents?.OnDialogStateChanged?.Invoke(true);
            buttons[0].gameObject.SetActive(true);
            buttons[1].gameObject.SetActive(true);
            buttons[2].gameObject.SetActive(true);
            buttons[3].gameObject.SetActive(true);

            this.message.text = message;
            buttons[0].onClick.AddListener(confirmX);
            buttons[0].onClick.AddListener(Clear);
            buttons[1].onClick.AddListener(rejectB);
            buttons[1].onClick.AddListener(Clear);
            buttons[2].onClick.AddListener(confirmY);
            buttons[2].onClick.AddListener(Clear);
            buttons[3].onClick.AddListener(confirmA);
            buttons[3].onClick.AddListener(Clear);
            
            
            this.messageDuration = messageDuration;


            StringBuilder sbButtonText = new StringBuilder();
            sbButtonText.Append(tags[0]);
            sbButtonText.Replace("Confirm", buttonText.confirmX);
            buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append(tags[1]);
            sbButtonText.Replace("Reject", buttonText.rejectB);
            buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append(tags[2]);
            sbButtonText.Replace("Confirm", buttonText.confirmY);
            buttons[2].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            sbButtonText.Clear();
            sbButtonText.Append(tags[3]);
            sbButtonText.Replace("Confirm", buttonText.confirmA);
            buttons[3].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

            
        }
        else
        {
            Debug.LogWarning($"Cannot send message when current Session is active");
            Action action = () => {
                canvas.gameObject.SetActive(true);
                playerEvents?.OnDialogStateChanged?.Invoke(true);
                buttons[0].gameObject.SetActive(true);
                buttons[1].gameObject.SetActive(true);
                buttons[2].gameObject.SetActive(true);
                buttons[3].gameObject.SetActive(true);

                this.message.text = message;
                buttons[0].onClick.AddListener(confirmX);
                buttons[0].onClick.AddListener(Clear);
                buttons[1].onClick.AddListener(rejectB);
                buttons[1].onClick.AddListener(Clear);
                buttons[2].onClick.AddListener(confirmY);
                buttons[2].onClick.AddListener(Clear);
                buttons[3].onClick.AddListener(confirmA);
                buttons[3].onClick.AddListener(Clear);


                this.messageDuration = messageDuration;


                StringBuilder sbButtonText = new StringBuilder();
                sbButtonText.Append(tags[0]);
                sbButtonText.Replace("Confirm", buttonText.confirmX);
                buttons[0].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append(tags[1]);
                sbButtonText.Replace("Reject", buttonText.rejectB);
                buttons[1].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append(tags[2]);
                sbButtonText.Replace("Confirm", buttonText.confirmY);
                buttons[2].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();

                sbButtonText.Clear();
                sbButtonText.Append(tags[3]);
                sbButtonText.Replace("Confirm", buttonText.confirmA);
                buttons[3].transform.GetComponentInChildren<TextMeshProUGUI>().text = sbButtonText.ToString();
            };
            NextMessage.Add(action);
        }

    }





    // ── Layout helpers ────────────────────────────────────────────────────────
    // Arranges buttons in a D-pad / + shape centred below the message text.
    //
    //  Slot mapping:  buttons[0] = X → Left
    //                 buttons[1] = B → Right
    //                 buttons[2] = Y → Top
    //                 buttons[3] = A → Bottom
    //
    //  activeCount   visible slots
    //       1        X centred
    //       2        X left  |  B right
    //       3        X left  |  Y top  |  B right
    //       4        full +  (all four arms)
    //
    void LayoutButtons(int activeCount)
    {
        const float ARM_H = 105f;  // centre → left/right button centre
        const float ARM_V =  50f;  // centre → top/bottom button centre
        const float BTN_W = 120f;
        const float BTN_H =  36f;

        switch (activeCount)
        {
            case 1:
                // Single confirm — centred in the cross area
                SetBtn(0,      0f,    0f, BTN_W + 40f, BTN_H);
                break;

            case 2:
                // X left, B right
                SetBtn(0, -ARM_H,    0f, BTN_W, BTN_H);
                SetBtn(1,  ARM_H,    0f, BTN_W, BTN_H);
                break;

            case 3:
                // X left, Y top, B right — no bottom arm
                SetBtn(0, -ARM_H,    0f, BTN_W, BTN_H);
                SetBtn(2,     0f, ARM_V, BTN_W, BTN_H);
                SetBtn(1,  ARM_H,    0f, BTN_W, BTN_H);
                break;

            default:
                // Full + shape
                SetBtn(0, -ARM_H,     0f, BTN_W, BTN_H);  // X left
                SetBtn(1,  ARM_H,     0f, BTN_W, BTN_H);  // B right
                SetBtn(2,     0f,  ARM_V, BTN_W, BTN_H);  // Y top
                SetBtn(3,     0f, -ARM_V, BTN_W, BTN_H);  // A bottom
                break;
        }
    }

    void SetBtn(int idx, float x, float y, float w, float h)
    {
        var rt              = buttons[idx].GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);

        var lbl = buttons[idx].GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null)
        {
            lbl.enableAutoSizing = true;
            lbl.fontSizeMin      = 10f;
            lbl.fontSizeMax      = 18f;
            lbl.alignment        = TextAlignmentOptions.Center;
        }
    }

    public void Clear()
    {
        this.message.text = string.Empty;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.RemoveAllListeners();
        }
        timer = 0;
        canvas.gameObject.SetActive(false);

        if (NextMessage.Count > 0)
        {
            // Fire closed first so the state machine returns to the previous state
            // (Battle/Prone), then the queued message immediately re-opens dialog.
            playerEvents?.OnDialogStateChanged?.Invoke(false);
            NextMessage[0]?.Invoke();   // activates canvas and fires OnDialogStateChanged(true) internally
            NextMessage.RemoveAt(0);
        }
        else
        {
            // No queued message — dialog is truly closed.
            playerEvents?.OnDialogStateChanged?.Invoke(false);
        }
    }
    public Canvas GetCanvas()
    {
        return canvas;
    }

    public void DeactivateUI()
    {
        
           
        for (int i = 0; i < buttons.Length; i++)
        {
            
            buttons[i].onClick.RemoveAllListeners();
            
            //MessageBox.gameObject.SetActive(false);
        }
        
       
        canvas = null;
        message = null;         
        
        GameObject.Destroy(canvas.gameObject); 

    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        _isHitConfirmPause = true;
    }

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {

        _isHitConfirmPause = false;



    }

    
}

                
                

    

