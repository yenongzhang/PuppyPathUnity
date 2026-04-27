using UnityEngine;
using UnityEngine.InputSystem;

public class DogStateTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator dogAnimator;
    [SerializeField] private Renderer eyesRenderer;
    [SerializeField] private Renderer mouthRenderer;

    [Header("Eye Textures")]
    [SerializeField] private Texture happyEyes;
    [SerializeField] private Texture lookingAroundEyes;
    [SerializeField] private Texture angryEyes;
    [SerializeField] private Texture calmEyes;
    [SerializeField] private Texture tearfulEyes;
    [SerializeField] private Texture barkingEyes;
    [SerializeField] private Texture restingEyes;
    [SerializeField] private Texture smugEyes;

    [Header("Mouth Textures")]
    [SerializeField] private Texture happyMouth;
    [SerializeField] private Texture lookingAroundMouth;
    [SerializeField] private Texture angryMouth;
    [SerializeField] private Texture calmMouth;
    [SerializeField] private Texture tearfulMouth;
    [SerializeField] private Texture barkingMouth;
    [SerializeField] private Texture restingMouth;
    [SerializeField] private Texture smugMouth;

    private void Start()
    {
        if (dogAnimator == null)
            dogAnimator = GetComponentInChildren<Animator>();

        Debug.Log("DogStateTester ready. Press 1-8 to test dog states.");
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Calm / Standing");
            SetExpression(calmEyes, calmMouth);
            PlayStand();
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Happy / Walking");
            SetExpression(happyEyes, happyMouth);
            PlayWalk();
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Happy / Running");
            SetExpression(happyEyes, happyMouth);
            PlayRun();
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Angry / Sniffing");
            SetExpression(angryEyes, angryMouth);
            PlaySniffing();
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Barking");
            SetExpression(barkingEyes, barkingMouth);
            PlayBark();
        }

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Arrived");
            SetExpression(happyEyes, happyMouth);
            PlayArrive();
        }

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Resting / Lazy");
            SetExpression(restingEyes, restingMouth);
            PlayLazy();
        }

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            Debug.Log("Dog Test: Smug");
            SetExpression(smugEyes, smugMouth);
            PlayStand();
        }
    }

    private void SetExpression(Texture eyesTexture, Texture mouthTexture)
    {
        Debug.Log("SetExpression called");

        if (eyesRenderer == null)
        {
            Debug.LogWarning("Eyes Renderer is NULL");
        }
        else if (eyesTexture == null)
        {
            Debug.LogWarning("Eyes Texture is NULL");
        }
        else
        {
            Material eyesMat = eyesRenderer.material;

            if (eyesMat.HasProperty("_BaseMap"))
            {
                eyesMat.SetTexture("_BaseMap", eyesTexture);
                Debug.Log("Eyes _BaseMap changed to: " + eyesTexture.name);
            }
            else if (eyesMat.HasProperty("_MainTex"))
            {
                eyesMat.SetTexture("_MainTex", eyesTexture);
                Debug.Log("Eyes _MainTex changed to: " + eyesTexture.name);
            }
            else
            {
                eyesMat.mainTexture = eyesTexture;
                Debug.Log("Eyes mainTexture changed to: " + eyesTexture.name);
            }
        }

        if (mouthRenderer == null)
        {
            Debug.LogWarning("Mouth Renderer is NULL");
        }
        else if (mouthTexture == null)
        {
            Debug.LogWarning("Mouth Texture is NULL");
        }
        else
        {
            Material mouthMat = mouthRenderer.material;

            if (mouthMat.HasProperty("_BaseMap"))
            {
                mouthMat.SetTexture("_BaseMap", mouthTexture);
                Debug.Log("Mouth _BaseMap changed to: " + mouthTexture.name);
            }
            else if (mouthMat.HasProperty("_MainTex"))
            {
                mouthMat.SetTexture("_MainTex", mouthTexture);
                Debug.Log("Mouth _MainTex changed to: " + mouthTexture.name);
            }
            else
            {
                mouthMat.mainTexture = mouthTexture;
                Debug.Log("Mouth mainTexture changed to: " + mouthTexture.name);
            }
        }
    }

    private void ResetAnimationBools()
    {
        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsWalking", false);
        dogAnimator.SetBool("IsRunning", false);
        dogAnimator.SetBool("IsSniffing", false);
        dogAnimator.SetBool("IsStanding", false);
        dogAnimator.SetBool("IsWaggingTail", false);
    }

    private void PlayStand()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsStanding", true);
    }

    private void PlayWalk()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsWalking", true);
        dogAnimator.SetBool("IsWaggingTail", true);
    }

    private void PlayRun()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsRunning", true);
    }

    private void PlaySniffing()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetBool("IsSniffing", true);
    }

    private void PlayBark()
    {
        if (dogAnimator == null)
            return;

        dogAnimator.SetTrigger("Bark");
    }

    private void PlayArrive()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetTrigger("Arrive");
        dogAnimator.SetBool("IsWaggingTail", true);
    }

    private void PlayLazy()
    {
        ResetAnimationBools();

        if (dogAnimator == null)
            return;

        dogAnimator.SetTrigger("Lazy");
        dogAnimator.SetBool("IsStanding", true);
    }
}