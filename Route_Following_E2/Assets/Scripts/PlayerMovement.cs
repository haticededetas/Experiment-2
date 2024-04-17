using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour

{

    JoyStickControl JSinputs;
    Vector2 rotate; // to save rotation amount in the joystick
    Vector2 rotateLeft; // to save rotation amount in the joystick

    public enum CorrectAnswer
    {
        A,
        W,
        D,
        X
    }

    [System.Serializable]
    public struct DecisionPoint
    {
        public Transform target; // Target object
        public CorrectAnswer correctAnswer; // Correct answer for the decision point
        public Transform firstLook;
    }

    private bool isLookingAtFirstLook = false;
    // private bool reachedFirstLook = false;

    // Data save

    public CsvExporter csvExporter; // Reference to the CsvExporter script


    // Prepare the data list.

    class Trial_Data
    {
        public string Participant { get; set; } // Participant Number
        public string TestOrder { get; set; } // TestNumber
        public string MapName { get; set; } // MapName

        public string DP_Name { get; set; } // decision point name
        public int Index_Number { get; set; } // the index number        
        public float ErrorNumber { get; set; } // number of errors

        public float RT_rotation_correct { get; set; } // reaction time during rotation
        public float RT_decision_correct { get; set; } // reaction time during decision
        public float RT_correct { get; set; } // total reaction time during decision

        public float RT_rotation_mistake1 { get; set; } // reaction time during first rotation mistake
        public float RT_decision_mistake1 { get; set; } // reaction time during first decision mistake
        public float RT_mistake1 { get; set; } // total reaction time during first decision mistake

        public float RT_rotation_mistake2 { get; set; } // reaction time during second rotation mistake
        public float RT_decision_mistake2 { get; set; } // reaction time during second decision mistake
        public float RT_mistake2 { get; set; } // total reaction time during first decision mistake

        public float RT_rotation_mistake3 { get; set; } // reaction time during third rotation mistake
        public float RT_decision_mistake3 { get; set; } // reaction time during third decision mistake
        public float RT_mistake3 { get; set; } // total reaction time during first decision mistake

    }

    // Prepate file name with subID and time 
    public static string SubID; // I declared this here, at I have reference to it in the GetSubID script where I defined a function to get SubID


    // Combine formatted date and time with filename

    private string fileName; // define variable

    List<Trial_Data> data_list = new List<Trial_Data>(); // the list which consists of trial data





    public string filePath; // File path for the CSV file

    private string decision_point_name; // It will be used to save data correctly

    public DecisionPoint[] decisionPoints; // Array of decision points


    private float rotationSpeed = 15f; // Speed at which the player rotates towards the target
    private float rotationTimeLimit = 30f; // Time limit for rotation (in seconds)

    public NavMeshAgent navMeshAgent; // Reference to the NavMeshAgent component
    public AlertMessage alertMessageComponent; // Reference to the AlertMessage component


    // Create parameters for the start of the task
    private bool shouldMove = false; // Flag to control player movement
    private bool isRotating = false; // Flag to control player rotation
    private float rotationTimer = 0f; // Timer for rotation
    private float decisionTimer = 0f; // Timer for rotation
    private bool canMakeDecision = false; // Flag to allow decision-making
    private bool decCorrect = true; // If the decision is correct or not. Created to define the messages presented.

    private Quaternion initialRotation; // Initial rotation when arriving at target

    public Transform initialOrientation; // Object to determine the initial rotation


    //Create Indicies 
    private int currentDecisionIndex = 0; // Index of the current decision point
    private int numberofError = 0;  // Index for the number of errors

    // Create possible reaction times for each rotation movement
    private float RT_rotation_correct = 0; // RT for the first try
    private float RT_rotation_mistake1 = 0; // RT for the second try
    private float RT_rotation_mistake2 = 0; // RT for the third try
    private float RT_rotation_mistake3 = 0; // RT for the third try

    // Create possible reaction times for each decision process
    private float RT_decision_correct = 0;
    private float RT_decision_mistake1 = 0;
    private float RT_decision_mistake2 = 0;
    private float RT_decision_mistake3 = 0;

    // Create variables for total RTs.
    private float RT_correct = 0;
    private float RT_mistake1 = 0;
    private float RT_mistake2 = 0;
    private float RT_mistake3 = 0;

    
    // PREPARE JOYSTICK RESPONSES
    private void Awake()
    {

        
        // Reach joystick response actions and name them as inputs
        JSinputs = new JoyStickControl();
    }

    private void OnEnable()
    {
        JSinputs.Enable();
    }

    private void OnDisable()
    {
        JSinputs.Disable();
    }

    private void Start()
    {


        UnityEngine.Cursor.visible = false;

        // Get the name of the current scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        string testNumber = LoadLevel.i.ToString();

        fileName = "Data/" + SubID + "_" + "Test" + testNumber + "_" + currentSceneName + "_" + GetSubID.exptime + ".csv";

        // create 10 rows for the list


        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = 6f; // Adjust the value as needed
        navMeshAgent.autoBraking = false; // Disable auto-braking to allow for smooth rotation

        // Allow rotation from the start
        isRotating = true;



        // Set the initial rotation based on the initialOrientation object
        if (initialOrientation != null)
        {
            Vector3 direction = initialOrientation.position - transform.position;
            initialRotation = Quaternion.LookRotation(direction);

            // Apply the initial rotation immediately
            transform.rotation = initialRotation;
            isRotating = true;
        }


        // Check if the AlertMessage component is found
        if (alertMessageComponent == null)
        {
            Debug.LogError("AlertMessage component reference not set!");
        }

        //message
        string rotationMessage = "Bitte wenden Sie sich, um Ihre Antwortsmöglichkeiten zu sehen, und drücken Sie die Taste A, um mit der Entscheidungsphase fortzufahren.";
        alertMessageComponent.ShowAlert(rotationMessage);


    }

    private void Update()
    {
        
        // Define JoyStick Responses
        // Here I defined booleans to later call in the corresponding places.
        // These will be true if the corresponding keys are pressed.

        bool RotateL = JSinputs.Response.RotateLeft.ReadValue<float>() > 0.1; // Rotate left
        bool RotateR = JSinputs.Response.RotateRight.ReadValue<float>() > 0.1; // Rotate right

        bool MakeDec = JSinputs.Response.MakeDec.ReadValue<float>() > 0.1; // Move to the decision phase


        if (shouldMove)
        {
            JSinputs.Disable(); // disable Unity Inputs
            decisionTimer = 0f; // reset timer

            alertMessageComponent.HideAlert();
            // Check if the player has reached the current decision point
            if (navMeshAgent.enabled && !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                // Stop the player at the target destination
                navMeshAgent.isStopped = true;
                shouldMove = false;

                // Increment the current decision index
                currentDecisionIndex++;
                Debug.Log(currentDecisionIndex);

                // Check if all decision points have been reached
                if (currentDecisionIndex >= decisionPoints.Length)
                {
                    
                    Debug.Log("All decision points reached!");
                    UnityEngine.Cursor.visible = true;
                    shouldMove = false;


                    // SAVE DATA
                    // name data file
                    string directoryPath = @"C:\Users\VRLab\OneDrive - Universität Mannheim\Desktop\VR_Experiment_1";

                    string filePath = Path.Combine(directoryPath, fileName);


                    //SaveDecisionPointsToCSV();
                    csvExporter.ExportListToCsv(data_list, filePath);

                    SceneManager.LoadScene("Instructions");

                    //return;



                }

                // Set the initial rotation when arriving at the decision point
                initialRotation = transform.rotation;

                // Start the rotation timer
                rotationTimer = 0f;

                // Enable rotation for a limited time
                isRotating = true;

                // Disable decision-making until space button is pressed
                canMakeDecision = false;
            }
        }

        // Rotate the player using keyboard arrows for a limited time
        if (isRotating)
        {
            
            rotationTimer += Time.deltaTime;
            JSinputs.Enable(); // Enable JosStick Inputs

            if (decCorrect)
            {
                string rotationMessage = "Bitte wenden Sie sich, um Ihre Antwortsmöglichkeiten zu sehen, und drücken Sie die Taste A, um mit der Entscheidungsphase fortzufahren.";
                alertMessageComponent.ShowAlert(rotationMessage);

            }
            else if (!decCorrect)
            {
                string errorMessage = "Ihre Antwort ist falsch. Sie können sich nochmal wenden und eine neue Antwort geben, nachdem Sie die Taste A gedrückt haben.";
                alertMessageComponent.ShowAlert(errorMessage);
            }



            //Rotation Response for the keyboard 
            if (Input.GetKey(KeyCode.RightArrow) || RotateR)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.LeftArrow) || RotateL)
            {
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
            }


            


            // Check if the rotation time limit has been reached or space button is pressed
            if (rotationTimer >= rotationTimeLimit || Input.GetKeyDown(KeyCode.Space) || MakeDec)
            {

                transform.rotation = initialRotation;
                isRotating = false;

                // Enable decision-making
                canMakeDecision = true;

                // Display appropriate message based on condition
                string message = rotationTimer >= rotationTimeLimit ? "Ihre Zeit ist abgelaufen, bitte treffen Sie eine Entscheidung." : "Bitte treffen Sie eine Entscheidung.";
                alertMessageComponent.ShowAlert(message);

                
            }






        }

        // Check if the 'a', 'w', 'd', or 'x' key is pressed to start moving towards the next decision point
        if (canMakeDecision && currentDecisionIndex < decisionPoints.Length)
        {


            //bool TurnRight = JSinputs.Response.TurnRight.ReadValue<float>() == 1; // turn right decision
            //bool TurnLeft = JSinputs.Response.TurnLeft.ReadValue<float>() == 1; // turn left decision
            // bool GoSt = JSinputs.Response.Straight.ReadValue<float>() == 1; // go straight decision
            bool TurnRight = JSinputs.Response.TurnRight.WasReleasedThisFrame();
            bool TurnLeft = JSinputs.Response.TurnLeft.WasReleasedThisFrame();
            bool GoSt = JSinputs.Response.Straight.WasReleasedThisFrame();



            decisionTimer += Time.deltaTime;
            JSinputs.Enable(); // Enable JosStick Inputs

            DecisionPoint currentDecision = decisionPoints[currentDecisionIndex];

            // Check if the input is one of the allowed response options
            if (TurnLeft || GoSt || TurnRight || 
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.W) || 
                Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.X))
            {
                // Check if the response is correct
                if (((TurnLeft || Input.GetKeyDown(KeyCode.A)) && currentDecision.correctAnswer == CorrectAnswer.A) ||
                    ((GoSt || Input.GetKeyDown(KeyCode.W)) && currentDecision.correctAnswer == CorrectAnswer.W) ||
                    ((TurnRight || Input.GetKeyDown(KeyCode.D)) && currentDecision.correctAnswer == CorrectAnswer.D) ||
                    (Input.GetKeyDown(KeyCode.X) && currentDecision.correctAnswer == CorrectAnswer.X))
                {
                    isLookingAtFirstLook = true;
                    RotateTowardsNextDecisionPoint(currentDecision);
                    

                    decCorrect = true; //decision is correct                 

                    if (currentDecisionIndex == 0)
                    {
                        decision_point_name = "DP1";
                    }
                    else
                    {
                        decision_point_name = decisionPoints[currentDecisionIndex - 1].target.name;
                    }

                    RT_rotation_correct = rotationTimer;
                    RT_decision_correct = decisionTimer;
                    RT_correct = rotationTimer + decisionTimer;


                    data_list.Insert(currentDecisionIndex, new Trial_Data
                    {
                        Participant = SubID,
                        TestOrder = LoadLevel.i.ToString(),
                        MapName = SceneManager.GetActiveScene().name,
                        DP_Name = decision_point_name,
                        Index_Number = currentDecisionIndex,
                        ErrorNumber = numberofError,
                        RT_rotation_correct = RT_rotation_correct,
                        RT_decision_correct = RT_decision_correct,
                        RT_correct = RT_correct,

                        RT_rotation_mistake1 = RT_rotation_mistake1,
                        RT_decision_mistake1 = RT_decision_mistake1,
                        RT_mistake1 = RT_mistake1,

                        RT_rotation_mistake2 = RT_rotation_mistake2,
                        RT_decision_mistake2 = RT_decision_mistake2,
                        RT_mistake2 = RT_mistake2,


                        RT_rotation_mistake3 = RT_rotation_mistake3,
                        RT_decision_mistake3 = RT_decision_mistake3,
                        RT_mistake3 = RT_mistake3,

                    });

                    string directoryPath = @"C:\Users\VRLab\OneDrive - Universität Mannheim\Desktop\VR_Experiment_2";

                    // name the data file
                    string filePath = Path.Combine(directoryPath, fileName);
                    //string filePath = Path.Combine(Application.persistentDataPath, fileName);
                    //SaveDecisionPointsToCSV();
                    csvExporter.ExportListToCsv(data_list, filePath);


                    // reset the RTs and the error number
                    RT_rotation_correct = 0;
                    RT_decision_correct = 0;

                    RT_rotation_mistake1 = 0;
                    RT_rotation_mistake2 = 0;
                    RT_rotation_mistake3 = 0;

                    RT_decision_mistake1 = 0;
                    RT_decision_mistake2 = 0;
                    RT_decision_mistake3 = 0;

                    RT_correct = 0;
                    RT_mistake1 = 0;
                    RT_mistake2 = 0;
                    RT_mistake3 = 0;

                    numberofError = 0;
                }
                else
                {
                    numberofError++;

                    if (numberofError == 1)
                    {
                        RT_rotation_mistake1 = rotationTimer;
                        RT_decision_mistake1 = decisionTimer;
                        RT_mistake1 = RT_rotation_mistake1 + RT_decision_mistake1;
                    }
                    else if (numberofError == 2)
                    {
                        RT_rotation_mistake2 = rotationTimer;
                        RT_decision_mistake2 = decisionTimer;
                        RT_mistake2 = RT_rotation_mistake2 + RT_decision_mistake2;
                    }
                    else if (numberofError == 3)
                    {
                        RT_rotation_mistake3 = rotationTimer;
                        RT_decision_mistake3 = decisionTimer;
                        RT_mistake3 = RT_rotation_mistake3 + RT_decision_mistake3;
                    }

                    decCorrect = false;
                    string errorMessage = "Your decision is wrong. You can rotate again or make a new response after pressing to the Button A.";
                    alertMessageComponent.ShowAlert(errorMessage);

                    rotationTimer = 0f;
                    isRotating = true;
                    canMakeDecision = false; //decision is incorrect


                }

            }


            

        }


    }

    private void RotateTowardsNextDecisionPoint(DecisionPoint decision)
    {
        Vector3 lookDirection = decision.firstLook.position - transform.position;
        Quaternion targetLookRotation = Quaternion.LookRotation(lookDirection);

        StartCoroutine(RotateTowardsTarget(targetLookRotation, () =>
        {
            isLookingAtFirstLook = false; // Finished looking at firstLook

            // Now start moving towards the target directly
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(decision.target.position);
            shouldMove = true;
        }));
    }



    private IEnumerator RotateTowardsTarget(Quaternion targetRotation, System.Action onRotationComplete)
    {
        float elapsedTime = 0f;
        float rotationDuration = 1f; // Duration of rotation (adjust as needed)
        float rotationSpeed = 1f;    // Rotation speed parameter (adjust as needed)


        Quaternion startRotation = transform.rotation;

        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;

            // Calculate the interpolation factor based on the elapsed time
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);

            // Slerp between the start rotation and the target rotation
            transform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, 1 - Mathf.Exp(-rotationSpeed * t));


            yield return null;
        }

        // Set the final rotation to ensure accuracy
        transform.rotation = targetRotation;

        // Invoke the callback function when the rotation is complete
        onRotationComplete?.Invoke();
    }

    
    

}


