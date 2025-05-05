
using System.Collections.Generic;

using UnityEngine;
using System.IO;
using Image = UnityEngine.UI.Image;

public class auvMove : MonoBehaviour
{

    public Rigidbody auvRigidbody;

    //back motors (move you forwards and back)
    public Transform leftMotor;
    public Transform rightMotor;
    public float motorForce = 1f;
    public float turnMotorForce = 0.012f;

    //front motors and rotations
    public Transform frontLeftMotor;
    public Transform frontRightMotor;
    public float frontMotorForce = 1f;
    float xRotation;
    bool dontAllowRotate;
    private Quaternion lastRotation;

    //center of mass
    private GameObject centerOfMassSphere;

    //cameras underwater and above, plus fogg and underwater effect
    public Camera underwaterCam;
    public Camera abovewaterCam;
    public Camera finalCam;
    public bool isAboveWater = true;
    public GameObject underwatertint;

    //movement routine 
    private Vector3 targetDirection;
    private Vector3 startingPoint;

    //line search
    private bool linesearch = false;
    private int index;
    private int[] xPos = { -424, -365, 275, -207, -305, -223, -44, -63, 341, 339};
    private int[] zPos = { -382, -414, 420, -266, -437, -224, -311, -81, 337, 106 };

    //detection stuff
    public float detectionRange = 50f;
    public GameObject displayphotos;

    private List<Texture2D> screenshots = new List<Texture2D>();
    private List<Vector3> screenshotPositions = new List<Vector3>();  // List to store screenshots
    public LayerMask detectableLayer; //only gets marine life
    private HashSet<Collider> objectsInView = new HashSet<Collider>();
    
    //displaying images correctly
    public Image uiImageDisplay;
    public GameObject spherePrefab;
    private Dictionary<GameObject, Texture2D> sphereImageMap = new Dictionary<GameObject, Texture2D>();

    //grid search
    private bool gridsearch = false;
    public float gridSize = 120f;
    public Vector3 topLeft = new Vector3(-440f, 0f, 420f);  // Starting position (top-left)
    public Vector3 bottomRight = new Vector3(400f, 0f, -420f);
    private List<Vector3> gridPoints = new List<Vector3>();
    private int currentPointIndex = 0;
    private int currentPointIndexR = 0;
    
    bool startedReturn = true;
    private List<Vector3> successfullyVisited = new List<Vector3>();
    public GameObject pointPrefab; //test
    

    //spiral search
    private bool spiralsearch = false;
    private List<Vector3> spiralPoints = new List<Vector3>();
    private int spiralPointIndex = 0;
    
    //random 
    private bool randomsearch = false;
    private List<Vector3> randomPoints = new List<Vector3>();
    private int numRanPoints = 70;

    private void Start()
    {
        string screenshotsFolder = Path.Combine(Application.persistentDataPath, "Screenshots");
        if (Directory.Exists(screenshotsFolder))
        {
            // Delete all files in the folder
            string[] files = Directory.GetFiles(screenshotsFolder);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            Debug.Log("Screenshot folder emptied");
        }
        else
        {
            // If the folder doesn't exist, create it
            Directory.CreateDirectory(screenshotsFolder);
            Debug.Log("created");
        }


        //allow our up and down adjustments
        //main just angles camera movement in the direction doesnt really happen due to bouyancy
        dontAllowRotate = true;

        //setup for camera switching and underwaters
        underwatertint.SetActive(false);
        abovewaterCam.enabled = true;
        underwaterCam.enabled = false;
        finalCam.enabled = false;

        displayphotos.SetActive(false);

        //setup a starting point
        startingPoint = new Vector3(transform.position.x, 0, transform.position.z);

        //create a sphere at the center of mass
        centerOfMassSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        centerOfMassSphere.transform.localScale = Vector3.one * 0.1f;
        centerOfMassSphere.GetComponent<Collider>().enabled = false;
        centerOfMassSphere.GetComponent<MeshRenderer>().material.color = Color.red;

        GenerateGrid();
        GenerateRandomPoints();
        GenerateSpiral();
        //TestSpiral();
        spiralPointIndex = spiralPoints.Count - 1;
        
    }

    //this is for  allowing fo rthe center of masss to be visiable 
    void OnDrawGizmos()
    {
        if (auvRigidbody != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(auvRigidbody.worldCenterOfMass, 0.2f);
        }

    }


    private void Update()
    {

        //lock rotation
        xRotation = Vector3.SignedAngle(Vector3.up, transform.up, transform.right);

        //detect objects script
        DetectObjectsWithinRange();

        //this is for camera switching and applying the nesscessary underwater effects
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isAboveWater)
            {
                RenderSettings.fog = true;
                underwatertint.SetActive(true);
                abovewaterCam.enabled = false;
                underwaterCam.enabled = true;
                isAboveWater = false;
            }
            else
            {
                RenderSettings.fog = false;
                underwatertint.SetActive(false);
                abovewaterCam.enabled = true;
                underwaterCam.enabled = false;
                isAboveWater = true;
            }
        }
  
        //final veiw end search early
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Spacebar pressed");
            uiImageDisplay.gameObject.SetActive(false);
            finalview();
            //TestGrid();


        }

        //starting pathing if pressed --- line search
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (!linesearch)
            {
                linesearch = true;
                index = 0;
            }
        }

        //starting pathing if pressed ---- grid search
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!gridsearch)
            {
                gridsearch = true;
                index = 0;
                successfullyVisited.Add(startingPoint);
                //TestGrid();
            }
        }

        //starting pathing if pressed ---- spiral search
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (!spiralsearch)
            {
                spiralsearch = true;
            }
        }

        //starting pathing if pressed ---- Random search
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!randomsearch)
            {
                randomsearch = true;
                //TestRan();
                //Debug.Log("pressed");
            }
        }


    }
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++fixed update ++++++++++++++++++++++++++++++++++++++++++++++
    private void FixedUpdate()
    {

        //test speeds out of water
        /*Vector3 pos = transform.position;
        pos.y = 2;
        transform.position = pos;*/

       
        //testing movements
        // Forward movement
        if (Input.GetKey(KeyCode.W))
        {
            ApplyForce(leftMotor.position, transform.forward * motorForce);
            ApplyForce(rightMotor.position, transform.forward * motorForce);
        }
        // Backward movement
        if (Input.GetKey(KeyCode.S))
        {
            ApplyForce(leftMotor.position, -transform.forward * motorForce);
            ApplyForce(rightMotor.position, -transform.forward * motorForce);
        }
        // Turn Left
        if (Input.GetKey(KeyCode.A))
        {
            ApplyForce(leftMotor.position, -transform.forward * motorForce);
            ApplyForce(rightMotor.position, transform.forward * motorForce);
        }
        // Turn Right
        if (Input.GetKey(KeyCode.D))
        {
            ApplyForce(leftMotor.position, transform.forward * motorForce);
            ApplyForce(rightMotor.position, -transform.forward * motorForce);
        }

        //pithc controls ----
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
        {
            dontAllowRotate = false;
        }
        else
        {
            dontAllowRotate = true;
        }
        if (dontAllowRotate)
        {
            Vector3 lockedEuler = lastRotation.eulerAngles;
            transform.rotation = Quaternion.Euler(lockedEuler.x, transform.rotation.eulerAngles.y, lockedEuler.z);
            Vector3 angVel = auvRigidbody.angularVelocity;
            angVel.x = 0;
            angVel.z = 0;
            auvRigidbody.angularVelocity = angVel;
        }
        else
        {
            //Pitch Up
            if (Input.GetKey(KeyCode.UpArrow))
            {
                ApplyForce(frontLeftMotor.position, transform.up * frontMotorForce);
                ApplyForce(frontRightMotor.position, transform.up * frontMotorForce);
            }
            // Pitch Down
            if (Input.GetKey(KeyCode.DownArrow))
            {
                ApplyForce(frontLeftMotor.position, -transform.up * frontMotorForce);
                ApplyForce(frontRightMotor.position, -transform.up * frontMotorForce);
            }
            lastRotation = transform.rotation;
        }

        //if searching do correct movement ----
        if (linesearch)
        {
            lineMoveTowardsTarget();
        }

        if (gridsearch)
        {
            gridMoveTowardsTarget();
        }

        if (spiralsearch)
        {
            SpiralUpdateTarget();
        }

        if (randomsearch)
        {
            randomMoveTowardsTarget();
        }
    }
    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++ fixed update ++++++++++++++++++++++++++++++++++++++++++++++

    // ------------------------------------------------------- movement ------------------------------------------------
    void lineUpdateTarget()
    {
        if (index < xPos.Length)
        {
            targetDirection = new Vector3(xPos[index], transform.position.y, zPos[index]);
        }
        else if (Vector3.Distance(transform.position, startingPoint) < 5f)
        {
            linesearch = false;
            finalview();
        }
        else
        {
            targetDirection = startingPoint;
        }
    }

    //movement model 1 line movement specific locaiton to specific location, tailored to the specific area being used
    void lineMoveTowardsTarget()
    {
        //get current set up and angel correctly
        Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 travelDirection = (targetDirection - transform.position).normalized;
        float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);


        if (angle > 5f)
        {
            ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
            ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
        }
        else if (angle < -5f)
        {
            ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
            ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
        }

        else
        {
            ApplyForce(leftMotor.position, transform.forward * motorForce);
            ApplyForce(rightMotor.position, transform.forward * motorForce);
        }

        if (Vector3.Distance(transform.position, targetDirection) < 5f)
        {
            index++;
            lineUpdateTarget();
        }

    }

    void gridMoveTowardsTarget()
    {
        if (currentPointIndex < gridPoints.Count)
        {
            Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetPoint = gridPoints[currentPointIndex];
            Vector3 travelDirection = (targetPoint - transform.position).normalized;
            float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

            //Debug.Log($"retracing {targetPoint}");

            if (angle > 5f)
            {
                ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
            }
            else if (angle < -5f)
            {
                ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
            }

            else
            {
                if (Vector3.Distance(transform.position, targetPoint) < 10f)
                {
                    Debug.Log($"hit point {currentPointIndex}");
                    successfullyVisited.Add(targetPoint);
                    currentPointIndex++;

                }

                if (IsTerrainAhead(targetPoint))
                {
                    currentPointIndex++;
                    Debug.Log($"skip point {currentPointIndex}" );
                    return;
                }
              
                ApplyForce(leftMotor.position, transform.forward * motorForce);
                ApplyForce(rightMotor.position, transform.forward * motorForce);
                
                
            }
        }
        else
        {
            //Debug.Log($"STARTING RETRACE");
            if (startedReturn)
            {
                currentPointIndexR = successfullyVisited.Count - 2;
                startedReturn = false;
                //Debug.Log($"STARTING RETRACE 2");
            }
            if (currentPointIndexR >= 0)
            {
                
                Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Vector3 targetPoint = successfullyVisited[currentPointIndexR];
                Vector3 travelDirection = (targetPoint - transform.position).normalized;
                float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

                //Debug.Log($"retracing {targetPoint} ----- {currentPointIndexR}");

                if (angle > 5f)
                {
                    ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
                }
                else if (angle < -5f)
                {
                    ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
                }

                else
                {
                    if (Vector3.Distance(transform.position, targetPoint) < 10f)
                    {
                        Debug.Log($"hit point {currentPointIndexR} R");
                        currentPointIndexR--;
                    }
                    ApplyForce(leftMotor.position, transform.forward * motorForce);
                    ApplyForce(rightMotor.position, transform.forward * motorForce);
                }
            }
            if (Vector3.Distance(transform.position, startingPoint) < 5f)
            {
                Debug.Log($"ending grid");
                gridsearch = false;
                finalview();
            }
        }
    }

    void SpiralUpdateTarget()
    {
        if (spiralPointIndex >= 0)
        {
            Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetPoint = spiralPoints[spiralPointIndex];
            Vector3 travelDirection = (targetPoint - transform.position).normalized;
            float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

            //Debug.Log($"{spiralPointIndex}");

            if (angle > 5f)
            {
                ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
            }
            else if (angle < -5f)
            {
                ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
            }

            else
            {
                if (Vector3.Distance(transform.position, targetPoint) < 10f)
                {
                    Debug.Log($"hit point {spiralPointIndex}");
                    successfullyVisited.Add(targetPoint);
                    spiralPointIndex--;

                }

                if (IsTerrainAhead(targetPoint))
                {
                    spiralPointIndex++;
                    Debug.Log($"skip point {spiralPointIndex}");
                    return;
                }

                ApplyForce(leftMotor.position, transform.forward * motorForce);
                ApplyForce(rightMotor.position, transform.forward * motorForce);


            }
        }
        else
        {
            Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 travelDirection = (startingPoint - transform.position).normalized;
            float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

            if (angle > 5f)
            {
                ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
            }
            else if (angle < -5f)
            {
                ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
            }
            else
            {
                ApplyForce(leftMotor.position, transform.forward * motorForce);
                ApplyForce(rightMotor.position, transform.forward * motorForce);
            }
            if (Vector3.Distance(transform.position, startingPoint) < 5f)
            {
                Debug.Log($"ending grid");
                spiralsearch = false;
                finalview();
            }
        }
    }


    //--- random movemnet section
    void randomMoveTowardsTarget()
    {
        if (currentPointIndex < randomPoints.Count)
        {
            Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 targetPoint = randomPoints[currentPointIndex];
            Vector3 travelDirection = (targetPoint - transform.position).normalized;
            float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);


            if (angle > 5f)
            {
                ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
            }
            else if (angle < -5f)
            {
                ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
            }

            else
            {
                if (Vector3.Distance(transform.position, targetPoint) < 10f)
                {
                    Debug.Log($"hit point {currentPointIndex}");
                    successfullyVisited.Add(targetPoint);
                    currentPointIndex++;

                }

                if (IsTerrainAhead(targetPoint))
                {
                    currentPointIndex++;
                    Debug.Log($"skip point {currentPointIndex}");
                    return;
                }

                ApplyForce(leftMotor.position, transform.forward * motorForce);
                ApplyForce(rightMotor.position, transform.forward * motorForce);


            }
        }
        else
        {
            if (startedReturn)
            {
                currentPointIndexR = successfullyVisited.Count - 2;
                startedReturn = false;
            }
            if (currentPointIndexR >= 0)
            {

                Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Vector3 targetPoint = successfullyVisited[currentPointIndexR];
                Vector3 travelDirection = (targetPoint - transform.position).normalized;
                float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

                if (angle > 5f)
                {
                    ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
                }
                else if (angle < -5f)
                {
                    ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
                }

                else
                {
                    if (Vector3.Distance(transform.position, targetPoint) < 10f)
                    {
                        Debug.Log($"hit point {currentPointIndexR} R");
                        currentPointIndexR--;
                    }
                    ApplyForce(leftMotor.position, transform.forward * motorForce);
                    ApplyForce(rightMotor.position, transform.forward * motorForce);
                }
            }
            else
            {
                Vector3 currentDireciton = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Vector3 travelDirection = (startingPoint - transform.position).normalized;
                float angle = Vector3.SignedAngle(currentDireciton, travelDirection, Vector3.up);

                if (angle > 5f)
                {
                    ApplyForce(leftMotor.position, transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, -transform.forward * turnMotorForce);
                }
                else if (angle < -5f)
                {
                    ApplyForce(leftMotor.position, -transform.forward * turnMotorForce);
                    ApplyForce(rightMotor.position, transform.forward * turnMotorForce);
                }
                else
                {
                    ApplyForce(leftMotor.position, transform.forward * motorForce);
                    ApplyForce(rightMotor.position, transform.forward * motorForce);
                }
            }

            if (Vector3.Distance(transform.position, startingPoint) < 10f)
            {
                Debug.Log($"ending grid");
                randomsearch = false;
                finalview();
            }
        }
    }

    void ApplyForce(Vector3 position, Vector3 force)
    {
        auvRigidbody.AddForceAtPosition(force, position);
    }

    // ------------------------------------------------------- movement ------------------------------------------------

    //end of movement

    //get image detection
    void DetectObjectsWithinRange()
    {

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(underwaterCam);


        Collider[] colliders = Physics.OverlapSphere(underwaterCam.transform.position, detectionRange, detectableLayer);

        foreach (Collider collider in colliders)
        {

            if (GeometryUtility.TestPlanesAABB(planes, collider.bounds))
            {
                float distance = Vector3.Distance(underwaterCam.transform.position, collider.transform.position);

                if (distance <= 14f && !objectsInView.Contains(collider))
                {
                    objectsInView.Add(collider);
                    CaptureScreenshot();
                }
            }
            else
            {
                if (objectsInView.Contains(collider))
                {
                    objectsInView.Remove(collider);
                }
            }
        }
    }

    void CaptureScreenshot()
    {
        Vector3 position = transform.position;
        string fileName = $"screenshot_{position.x}_{position.z}.png";
        string screenshotsFolder = Path.Combine(Application.persistentDataPath, "Screenshots");

        if (!Directory.Exists(screenshotsFolder))
        {
            Directory.CreateDirectory(screenshotsFolder);
        }
        string filePath = Path.Combine(screenshotsFolder, fileName);

        ScreenCapture.CaptureScreenshot(filePath);

        Debug.Log("Screenshot taken: " + filePath);
    }

    void finalview()
    {
        RenderSettings.fog = false;
        underwatertint.SetActive(false);
        abovewaterCam.enabled = false;
        underwaterCam.enabled = false;
        finalCam.enabled = true;
        finalCam.transform.position = new Vector3(70, 500, -70);
        displayphotos.SetActive(true);

        finalCam.orthographic = true;
        finalCam.orthographicSize = 400;

        LoadScreenshotPositions();
        PlaceSpheresAtScreenshotPositions();
    }

    void LoadScreenshotPositions()
    {
        string screenshotsFolder = Path.Combine(Application.persistentDataPath, "Screenshots");

        if (Directory.Exists(screenshotsFolder))
        {
            string[] screenshotFiles = Directory.GetFiles(screenshotsFolder, "screenshot_*.png");

            foreach (string file in screenshotFiles)
            {
                
                string fileName = Path.GetFileNameWithoutExtension(file);

                string[] parts = fileName.Split('_');
                if (parts.Length == 3)  
                {
                    //break up parts based on _ using the parts as positions
                    float xPosShot = float.Parse(parts[1]);
                    float zPosShot = float.Parse(parts[2]);
                    Debug.Log("got position added " + parts[1] + " _ " + parts[2]);
                    screenshotPositions.Add(new Vector3(xPosShot, 0, zPosShot));

                    // Load the image as a Texture2D
                    byte[] fileData = File.ReadAllBytes(file);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    screenshots.Add(texture);

                }
            }
        }   
    }
    void PlaceSpheresAtScreenshotPositions()
    {
        for (int i = 0; i < screenshotPositions.Count; i++)
        {
            Vector3 position = screenshotPositions[i];

            // Instantiate a sphere at the corresponding position
            GameObject locationSphere = Instantiate(spherePrefab, position, Quaternion.identity);
            locationSphere.transform.localScale = new Vector3(10f, 10f, 10f);

            // Check if there's a matching screenshot
            if (i < screenshots.Count)
            {
                Texture2D correspondingScreenshot = screenshots[i];

                // Store the screenshot in the dictionary
                sphereImageMap[locationSphere] = correspondingScreenshot;

                SphereHoverHandler hoverHandler = locationSphere.AddComponent<SphereHoverHandler>();
                hoverHandler.Initialize(correspondingScreenshot, uiImageDisplay);
            }
            else
            {
                Debug.LogWarning("No matching screenshot found for sphere at: " + position);
            }
        }
    }


    void GenerateGrid()
    {
        float xStart = topLeft.x;
        float zStart = topLeft.z;
        float xEnd = bottomRight.x;
        float zEnd = bottomRight.z;

        int gridWidth = Mathf.CeilToInt((xEnd - xStart) / gridSize);
        int gridLength = Mathf.CeilToInt((zStart - zEnd) / gridSize);

        for (int z = 0; z <= gridWidth; z++)
        {
            for (int x = 0; x <= gridLength; x++)
            {
                // Calculate the position of each grid point
                float xPos = xStart + x * gridSize;
                float zPos = zStart - z * gridSize;
                
                Vector3 point = new Vector3(xPos, 0, zPos);
                gridPoints.Add(point);
            }
        }
    }
    void TestGrid()
    {
        foreach (Vector3 point in gridPoints)
        {
            Instantiate(pointPrefab, point, Quaternion.identity);
        }
    }

    void GenerateRandomPoints()
    {
        randomPoints.Clear();  

        for (int i = 0; i < numRanPoints; i++)
        {
            
            float x = Random.Range(topLeft.x, bottomRight.x);
            float z = Random.Range(bottomRight.z, topLeft.z);
            Vector3 point = new Vector3(x, 0f, z);
            randomPoints.Add(point);
        }
    }
    void TestRan()
    {
        foreach (Vector3 point in randomPoints)
        {
            Instantiate(pointPrefab, point, Quaternion.identity);
        }
    }

    void GenerateSpiral()
    {
        
        float xStart = 254; //target point to hit 
        float zStart = 258;
        float radius = 0f;
        //build-up of spiral 
        float angle = 7f; //where spiral starts
        float angleIncrement = -5f; //turning angel
        float radiusIncrement = 1f; //distance buildup between points


        int maxPoints = 200;
        for (int i = 0; i < maxPoints; i++)
        {
            float xPos = xStart + radius * Mathf.Cos(angle);
            float zPos = zStart + radius * Mathf.Sin(angle);

            Vector3 point = new Vector3(xPos, 0, zPos);
            spiralPoints.Add(point);

            angle += angleIncrement * Mathf.Deg2Rad;
            radius += radiusIncrement;
        }
    }
    void TestSpiral()
    {
        foreach (Vector3 point in spiralPoints)
        {
            Instantiate(pointPrefab, point, Quaternion.identity);
        }
    }
    bool IsTerrainAhead(Vector3 target)
    {
        // Use raycasting to check if terrain is ahead of the submarine
        Vector3 direction = (target - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, distance)) //send out raycast in direction of point
        {
            if (hit.collider.CompareTag("Terrain")) 
            {
                // If terrain is detected, return true
                return true;
            }
        }
        // No terrain 
        return false;
    }

    

    //EOF
}


