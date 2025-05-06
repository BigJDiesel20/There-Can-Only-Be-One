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
    Rewired.Player gamePad;
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    public void OnUpdate()
    {
        if (canvas.enabled)
        {
            if (gamePad.GetButtonDown("X") & buttons[0].gameObject.activeSelf) buttons[0].onClick.Invoke();
            if (gamePad.GetButtonDown("B") & buttons[1].gameObject.activeSelf) buttons[1].onClick.Invoke();
            if (gamePad.GetButtonDown("Y") & buttons[2].gameObject.activeSelf) buttons[2].onClick.Invoke();
            if (gamePad.GetButtonDown("A") & buttons[3].gameObject.activeSelf) buttons[3].onClick.Invoke();
        }

        //Debug.Log($"{canvas.gameObject.name} is {canvas.gameObject.activeSelf}");

        if (canvas.gameObject.activeSelf == true) { if ((timer += Time.deltaTime / messageDuration) >= 1) Clear();  }
        
        
    }

    public void InstatiateMessageBox(Camera camera, Canvas canvas, Rewired.Player gamePad)
    {
        this.gamePad = gamePad;
        this.canvas = canvas;
        
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.sortingOrder = 10;
        canvas.planeDistance = 1;
        buttons = new Button[4];
        MessageBox = canvas.transform.Find("MessageBox").GetComponent<RectTransform>();
        Vector3 newAnchoredPositon = MessageBox.anchoredPosition;
        newAnchoredPositon.y -= 0;
        MessageBox.anchoredPosition = newAnchoredPositon;
        string[] tags = new string[buttons.Length];
        MessageBox.sizeDelta = new Vector3(3,3,3);

        tags[0] = "Confirm: (X)"; 
        tags[1] = "Reject: (B)"; 
        tags[2] = "Confirm: (Y)"; 
        tags[3] = "Confirm: (A)";

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = GameObject.FindGameObjectWithTag(tags[i]).GetComponent<Button>();
        }
        message = MessageBox.transform.Find("Message").GetComponentInChildren<TextMeshProUGUI>();

        
        canvas.transform.position = camera.transform.position + (Vector3.forward * 5);

        //canvas = new GameObject("Canvas",typeof(RectTransform)).AddComponent<Canvas>();
        //canvas.AddComponent<CanvasScaler>();
        //canvas.AddComponent<GraphicRaycaster>();
        //canvas.GetComponent<RectTransform>().sizeDelta = new Vector3(1, 1, 1);
        //canvas.renderMode = RenderMode.ScreenSpaceCamera;
        //canvas.worldCamera = camera;

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

    }

    //public void SetMessage(Message message)

    //{

    //}

    public void SetMessage(string message, UnityAction confirmX, string confirmXButtonText, double messageDuration)
    {
        if (!canvas.gameObject.activeSelf)
        { 
            canvas.gameObject.SetActive(true);
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

            
        }
        else
        {
            Debug.LogWarning($"Cannot send message when current Session is active");


            Action action = () =>
            {
                canvas.gameObject.SetActive(true);
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
            canvas.gameObject.SetActive(true);
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
            canvas.gameObject.SetActive(true);
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
            canvas.gameObject.SetActive(true);
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
            NextMessage[0]?.Invoke();
            NextMessage?.RemoveAt(0);
            
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

    public void OnHitConfirm((LocalPlayerManager hitBoxOwner, LocalPlayerManager hurtBoxOwner) hitInfo)
    {
        _isHitConfirmPause = true;
    }

    internal void OnHitPauseEnd(Vector3 forceDirection)
    {
        _isHitConfirmPause = false;
    }
}

                
                

    

