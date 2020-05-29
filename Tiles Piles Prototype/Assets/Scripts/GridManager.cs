using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using TMPro;


public enum Ingredients
{
    Empty,
    Bread,
    Bacon,
    Cheese,
    Egg,
    Ham,
    Onion,
    Salad,
    Salami,
    Tomato
}

public struct GridCell
{
    public Vector3 Center;
    public Vector3 rightBound;
    public Vector3 leftBound;
    public Vector3 bottomBound;
    public Vector3 topBound;
    public List<GameObject> ingredientsOnCell;
    public int objectInCellCounter;

    public GridCell(Vector3 center)
    {
        //coordinates setup
        Center = center;
        rightBound = new Vector3(center.x + 1, 0, center.z);
        leftBound = new Vector3(center.x - 1, 0, center.z);
        bottomBound = new Vector3(center.x, 0, center.z - 1);
        topBound = new Vector3(center.x, 0, center.z + 1);
        ingredientsOnCell = new List<GameObject>();

        //counter setup
        objectInCellCounter = 0;
    }
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Objects")]
    public bool useDefaulLevel;
    public int userDifficultyLevel;
    //nr of pieces excluded 2 bread pieces
    public Vector3 topLeftGridCellCenter = new Vector3(-3, 0, 3);
    public GridCell[,] grid = new GridCell[4, 4];
    public GameObject[] ingredientsObject;
    public Ingredients[] chooseIngredientsPosition = new Ingredients[16];
    public float movementSpeed = 2;
    public int angle = 90;
    public TextMeshProUGUI debugUI;
    public TextMeshProUGUI victoryUI;

    //private references
    public static GridManager instance;
    RaycastHit hit;
    Ray ray;
    private Vector3 actualCenter;
    private int cellCounter;
    private GameObject selectedIngredient;
    private GameObject destinationIngredient;
    private Vector3 mouseStartPosition;
    private Vector3 direction;
    private Vector3 newIngredientPosition;
    private bool movePiece;
    private int rowIndex;
    private int colIndex;
    private Vector3 destination;
    private Vector3 actualRotation;
    private Vector3 finalRotationDir;
    private bool victoryCondition = false;
    private bool otherCellsWithIngredient = false;
    public bool rotDone { get; set; }
    private bool movingIngredient;
    private bool victoryAchieved;

    //touchprivate
    private Vector3 startPosition;
    private Vector3 endPosition;

    //level generation
    private int difficultyLevel = 0;
    private int randomNum = 0;
    private int breadCounter;
    private int cellIndex;
    private int[] indexes = new int[2];
    private Vector2[] availablePos;
    private int[] unavailablePos;
    private int unavailableCounter;
    private int totalCounter;
    private bool breakCicle;
    private int otherIngredientsNum;
    private Ingredients[] otherIngredients;
    private bool startGame;


    private void Awake()
    {
        if (instance != null && instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }


    void Start()
    {
        if (useDefaulLevel)
        {
            GenerateDefaultLevel();
        }
        else
        {
            GenerateRandomLevel();
        }
    }
    void Update()
    {
        if (startGame)
        {
            if (!victoryAchieved)
            {
                if (!movingIngredient)
                {
                    DetectInput();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (movePiece)
        {
            MovePiece();
        }
    }

    public void StartGame()
    {
        startGame = true;
        Time.timeScale = 1;
        UIManager.instance.startGame.SetActive(false);
        UIManager.instance.reloadLevel.SetActive(true);
        UIManager.instance.loadLevel.SetActive(false);
        UIManager.instance.loadInputField.gameObject.SetActive(false);
        UIManager.instance.saveLevel.SetActive(false);
        UIManager.instance.saveInputField.gameObject.SetActive(false);
        UIManager.instance.newLevel.SetActive(false);
        UIManager.instance.plusBtn.SetActive(false);
        UIManager.instance.minusBtn.SetActive(false);
    }

    /// <summary>
    /// Generates the level following inspector instructions
    /// </summary>
    private void GenerateDefaultLevel()
    {
        //grid setup
        cellCounter = 0;
        actualCenter = topLeftGridCellCenter;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                grid[row, col] = new GridCell(actualCenter);

                if (chooseIngredientsPosition[cellCounter] != Ingredients.Empty)
                {
                    GameObject ingredient = Instantiate(ingredientsObject[(int)chooseIngredientsPosition[cellCounter] - 1], new Vector3(actualCenter.x, 0.05f, actualCenter.z), Quaternion.identity);
                    grid[row, col].objectInCellCounter++;
                    grid[row, col].ingredientsOnCell.Add(ingredient);
                }
                cellCounter++;

                if (col != 3)
                {
                    actualCenter = new Vector3(actualCenter.x + 2, 0, actualCenter.z);
                }
            }
            actualCenter = new Vector3(actualCenter.x - 6, 0, actualCenter.z - 2);
        }
    }

    /// <summary>
    /// Generates a random level
    /// </summary>
    public void GenerateRandomLevel()
    {
        //grid initial setup
        difficultyLevel = userDifficultyLevel;
        UIManager.instance.difficoultyText.text = difficultyLevel.ToString();
        chooseIngredientsPosition = new Ingredients[16];
        actualCenter = topLeftGridCellCenter;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                grid[row, col] = new GridCell(actualCenter);

                if (col != 3)
                {
                    actualCenter = new Vector3(actualCenter.x + 2, 0, actualCenter.z);
                }
            }
            actualCenter = new Vector3(actualCenter.x - 6, 0, actualCenter.z - 2);
        }

        //setup first ingredient on grid cell 
        randomNum = UnityEngine.Random.Range(0, 16);
        cellCounter = 0;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (randomNum == cellCounter)
                {
                    chooseIngredientsPosition[cellCounter] = Ingredients.Bread;
                    breadCounter++;
                    cellIndex = cellCounter;
                    difficultyLevel--;
                    indexes[0] = row;
                    indexes[1] = col;
                }
                cellCounter++;
            }
        }

        //setup other ingredients on cell based on difficulty level
        unavailablePos = new int[16];
        unavailableCounter = 0;

        while (difficultyLevel >= 1)
        {
            SetupAvailablePosition(indexes);
        }

        #region INSTANTIATE
        actualCenter = topLeftGridCellCenter;
        cellCounter = 0;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (chooseIngredientsPosition[cellCounter] != Ingredients.Empty)
                {
                    GameObject ingredient = Instantiate(ingredientsObject[(int)chooseIngredientsPosition[cellCounter] - 1], new Vector3(actualCenter.x, 0.05f, actualCenter.z), Quaternion.identity);
                    grid[row, col].objectInCellCounter++;
                    grid[row, col].ingredientsOnCell.Add(ingredient);
                }
                cellCounter++;

                if (col != 3)
                {
                    actualCenter = new Vector3(actualCenter.x + 2, 0, actualCenter.z);
                }
            }
            actualCenter = new Vector3(actualCenter.x - 6, 0, actualCenter.z - 2);
        }
        #endregion
    }

    /// <summary>
    /// Setups an array with the 4 adiacient grid cell positions and checks if position are available
    /// </summary>
    /// <param name="indexPos"></param>
    private void SetupAvailablePosition(int[] indexPos)
    {
        Vector2[] positions = new Vector2[4];
        bool[] available = new bool[4];
        int[] newCellIndex = new int[4];

        if (indexes[0] - 1 >= 0)
        {
            if (chooseIngredientsPosition[cellIndex - 4] == Ingredients.Empty)
            {
                positions[0] = new Vector2(indexes[0] - 1, indexes[1]); //up cell
                available[0] = true;
                newCellIndex[0] = cellIndex - 4;
            }
            else
            {
                available[0] = false;
            }
        }
        else
        {
            available[0] = false;
        }

        if (indexes[1] + 1 < 4 && chooseIngredientsPosition[cellIndex + 1] == Ingredients.Empty)
        {
            positions[1] = new Vector2(indexes[0], indexes[1] + 1); //right cell
            available[1] = true;
            newCellIndex[1] = cellIndex + 1;
        }
        else
        {
            available[1] = false;
        }

        if (indexes[0] + 1 < 4 && chooseIngredientsPosition[cellIndex + 4] == Ingredients.Empty)
        {
            positions[2] = new Vector2(indexes[0] + 1, indexes[1]); //down cell
            available[2] = true;
            newCellIndex[2] = cellIndex + 4;
        }
        else
        {
            available[2] = false;
        }

        if (indexes[1] - 1 >= 0 && chooseIngredientsPosition[cellIndex - 1] == Ingredients.Empty)
        {
            positions[3] = new Vector2(indexes[0], indexes[1] - 1); //left cell
            available[3] = true;
            newCellIndex[3] = cellIndex - 1;
        }
        else
        {
            available[3] = false;
        }

        cellCounter = 0;
        for (int i = 0; i < available.Length; i++)
        {
            if (available[i] == true)
            {
                cellCounter++;
            }
        }

        if (cellCounter == 0 || cellCounter == 4)
        {
            if (unavailableCounter > 0)
            {
                bool newUnavailable = false;
                for (int i = 0; i < unavailableCounter; i++)
                {
                    if (cellIndex == unavailablePos[i])
                    {
                        newUnavailable = false;
                        break;
                    }
                    else
                    {
                        newUnavailable = true;
                    }
                }

                if (newUnavailable)
                {
                    unavailablePos[unavailableCounter] = cellIndex;
                    unavailableCounter++;
                }
            }
            else
            {
                unavailablePos[unavailableCounter] = cellIndex;
                unavailableCounter++;
            }
        }

        if (cellCounter > 0 && cellCounter <4)
        {
            availablePos = new Vector2[cellCounter];

            int count = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                if (available[i] == true)
                {
                    availablePos[count] = positions[i];
                    count++;
                }
            }

            int random = UnityEngine.Random.Range(0, availablePos.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                if (availablePos[random] == positions[i])
                {
                    cellIndex = newCellIndex[i];
                }
            }
            if (breadCounter < 2)
            {
                chooseIngredientsPosition[cellIndex] = Ingredients.Bread;
                breadCounter++;
            }
            else
            {
                chooseIngredientsPosition[cellIndex] = (Ingredients)UnityEngine.Random.Range(2, 10);
            }

            indexes[0] = (int)availablePos[random].x;
            indexes[1] = (int)availablePos[random].y;
            unavailableCounter = 0;
            unavailablePos = new int[16];

            difficultyLevel--;
        }
        else
        {

            Debug.Log("No more available position. Choosing a new start position");
            cellCounter = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (chooseIngredientsPosition[cellCounter] == Ingredients.Empty)
                    {
                        bool isAvaialble = false;


                        for (int i = 0; i < unavailableCounter; i++)
                        {
                            if (cellCounter == unavailablePos[i])
                            {
                                isAvaialble = false;
                                break;
                            }
                            else
                            {
                                isAvaialble = true;
                            }
                        }




                        if (isAvaialble)
                        {
                            if ((row - 1 >= 0 && chooseIngredientsPosition[cellCounter - 4] != Ingredients.Empty) &&
                                        (col + 1 < 4 && chooseIngredientsPosition[cellCounter + 1] != Ingredients.Empty) &&
                                        (col - 1 >= 0 && chooseIngredientsPosition[cellCounter - 1] != Ingredients.Empty) &&
                                        (row + 1 < 4 && chooseIngredientsPosition[cellCounter + 4] != Ingredients.Empty)
                                        )
                            {
                                cellIndex = cellCounter;
                                chooseIngredientsPosition[cellIndex] = (Ingredients)UnityEngine.Random.Range(2, 10);
                                unavailableCounter = 0;
                                unavailablePos = new int[16];
                                difficultyLevel--;
                                indexes[0] = row;
                                indexes[1] = col;
                                breakCicle = true;
                                return;

                            }
                            else if ((row - 1 >= 0 && chooseIngredientsPosition[cellCounter - 4] != Ingredients.Empty) ||
                                        (col + 1 < 4 && chooseIngredientsPosition[cellCounter + 1] != Ingredients.Empty) ||
                                        (col - 1 >= 0 && chooseIngredientsPosition[cellCounter - 1] != Ingredients.Empty) ||
                                        (row + 1 < 4 && chooseIngredientsPosition[cellCounter + 4] != Ingredients.Empty)
                                        )
                            {
                                cellIndex = cellCounter;
                                chooseIngredientsPosition[cellIndex] = (Ingredients)UnityEngine.Random.Range(2, 10);
                                unavailableCounter = 0; 
                                unavailablePos = new int[16];
                                difficultyLevel--;
                                indexes[0] = row;
                                indexes[1] = col;
                                breakCicle = true;
                                return;
                            }
                        }
                    }
                    cellCounter++;
                }
                if (difficultyLevel <= 0 || breakCicle)
                {
                    breakCicle = false;
                    break;
                }
            }
        }
    }


    /// <summary>
    /// Debug method
    /// </summary>
    private void DetectInput()
    {

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.transform != null)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (hit.transform.CompareTag("Ingredient"))
                        {
                            if (!selectedIngredient)
                            {
                                selectedIngredient = hit.transform.gameObject;
                                debugUI.text = "Touched " + selectedIngredient.name;
                                mouseStartPosition = Input.mousePosition;
                            }
                            else if (hit.transform.gameObject != selectedIngredient)
                            {
                                destinationIngredient = hit.transform.gameObject;
                                debugUI.text = "Touched " + selectedIngredient.name + " " + destinationIngredient.name;
                                direction = Input.mousePosition - mouseStartPosition;
                                direction = new Vector3(direction.x, 0, direction.y);
                                direction.Normalize();
                                CalculateMovement();
                            }
                        }
                        else if (selectedIngredient)
                        {
                            selectedIngredient = null;
                            debugUI.text = "Selection Cancelled";
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        ray = Camera.main.ScreenPointToRay(t.position);
                        if (Physics.Raycast(ray, out hit, 100f))
                        {
                            if (hit.transform != null)
                            {
                                if (hit.transform.CompareTag("Ingredient"))
                                {
                                    if (!selectedIngredient)
                                    {
                                        selectedIngredient = hit.transform.gameObject;
                                        debugUI.text = "Touched " + selectedIngredient.name;
                                        startPosition = t.position;
                                    }
                                }
                                else if (selectedIngredient)
                                {
                                    selectedIngredient = null;
                                    debugUI.text = "Selection Cancelled";
                                }
                            }
                        }
                        break;
                    case TouchPhase.Moved:
                        ray = Camera.main.ScreenPointToRay(t.position);
                        if (Physics.Raycast(ray, out hit, 100f))
                        {
                            if (hit.transform.CompareTag("Ingredient"))
                            {
                                if (selectedIngredient)
                                {
                                    if (hit.transform.gameObject != selectedIngredient)
                                    {
                                        destinationIngredient = hit.transform.gameObject;
                                        debugUI.text = "Touched " + selectedIngredient.name + " " + destinationIngredient.name;
                                    }
                                }
                            }
                        }
                        break;
                    case TouchPhase.Stationary:
                        break;
                    case TouchPhase.Ended:
                        if (selectedIngredient && destinationIngredient)
                        {
                            endPosition = t.position;
                            direction = endPosition - startPosition;
                            direction = new Vector3(direction.x, 0, direction.y);
                            direction.Normalize();
                            CalculateMovement();
                        }
                        else if (selectedIngredient && !destinationIngredient)
                        {
                            selectedIngredient = null;
                            debugUI.text = "Selection Cancelled";
                        }
                        break;
                    case TouchPhase.Canceled:
                        break;
                    default:
                        break;
                }
            }
        }

    }

    private void CalculateMovement()
    {
        movingIngredient = true;
        //move up
        if (Mathf.RoundToInt(direction.z) == 1)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (grid[row, col].objectInCellCounter - 1 >= 0)
                    {
                        if (selectedIngredient == grid[row, col].ingredientsOnCell[grid[row, col].objectInCellCounter - 1])
                        {
                            if (grid[row - 1, col].objectInCellCounter != 0 && grid[row - 1, col].ingredientsOnCell[grid[row - 1, col].objectInCellCounter - 1] == destinationIngredient)
                            {
                                newIngredientPosition = new Vector3(grid[row - 1, col].Center.x, 0.05f + (0.1f * grid[row - 1, col].objectInCellCounter), grid[row - 1, col].Center.z);
                                rowIndex = row;
                                colIndex = col;
                            }
                            else
                            {
                                selectedIngredient = null;
                                destinationIngredient = null;
                                movePiece = false;
                                movingIngredient = false;
                                return;
                            }
                        }
                    }
                }
            }

            grid[rowIndex, colIndex].objectInCellCounter--;
            grid[rowIndex - 1, colIndex].objectInCellCounter++;
            grid[rowIndex - 1, colIndex].ingredientsOnCell.Add(selectedIngredient);
            grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient);
            if (selectedIngredient.transform.childCount != 0)
            {
                for (int i = selectedIngredient.transform.childCount - 1; i >= 0; i--)
                {
                    if (selectedIngredient.transform.GetChild(i).transform.CompareTag("Ingredient"))
                    {
                        grid[rowIndex, colIndex].objectInCellCounter--;
                        grid[rowIndex - 1, colIndex].objectInCellCounter++;
                        grid[rowIndex - 1, colIndex].ingredientsOnCell.Add(selectedIngredient.transform.GetChild(i).gameObject);
                        grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient.transform.GetChild(i).gameObject);
                    }
                }
            }

            destination = grid[rowIndex, colIndex].topBound;
            actualRotation = new Vector3(180, 0, 0);
            finalRotationDir = new Vector3(selectedIngredient.transform.eulerAngles.x + 180, selectedIngredient.transform.eulerAngles.y, selectedIngredient.transform.eulerAngles.z);
            rowIndex = rowIndex - 1;
            movePiece = true;

        }
        //move down
        else if (Mathf.RoundToInt(direction.z) == -1)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (grid[row, col].objectInCellCounter - 1 >= 0)
                    {
                        if (selectedIngredient == grid[row, col].ingredientsOnCell[grid[row, col].objectInCellCounter - 1])
                        {
                            if (grid[row + 1, col].objectInCellCounter != 0 && grid[row + 1, col].ingredientsOnCell[grid[row + 1, col].objectInCellCounter - 1] == destinationIngredient)
                            {
                                newIngredientPosition = new Vector3(grid[row + 1, col].Center.x, 0.05f + 0.1f * grid[row + 1, col].objectInCellCounter, grid[row + 1, col].Center.z);
                                rowIndex = row;
                                colIndex = col;
                            }
                            else
                            {
                                selectedIngredient = null;
                                destinationIngredient = null;
                                movePiece = false;
                                movingIngredient = false;
                                return;
                            }
                        }
                    }
                }
            }
            grid[rowIndex, colIndex].objectInCellCounter--;
            grid[rowIndex + 1, colIndex].objectInCellCounter++;
            grid[rowIndex + 1, colIndex].ingredientsOnCell.Add(selectedIngredient);
            grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient);
            if (selectedIngredient.transform.childCount != 0)
            {
                for (int i = selectedIngredient.transform.childCount - 1; i >= 0; i--)
                {
                    if (selectedIngredient.transform.GetChild(i).transform.CompareTag("Ingredient"))
                    {
                        grid[rowIndex, colIndex].objectInCellCounter--;
                        grid[rowIndex + 1, colIndex].objectInCellCounter++;
                        grid[rowIndex + 1, colIndex].ingredientsOnCell.Add(selectedIngredient.transform.GetChild(i).gameObject);
                        grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient.transform.GetChild(i).gameObject);
                    }
                }
            }

            destination = grid[rowIndex, colIndex].bottomBound;
            actualRotation = new Vector3(-180, 0, 0);
            finalRotationDir = new Vector3(selectedIngredient.transform.eulerAngles.x - 180, selectedIngredient.transform.eulerAngles.y, selectedIngredient.transform.eulerAngles.z);
            rowIndex = rowIndex + 1;
            movePiece = true;
        }
        //move right
        else if (Mathf.RoundToInt(direction.x) == 1)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (grid[row, col].objectInCellCounter - 1 >= 0)
                    {
                        if (selectedIngredient == grid[row, col].ingredientsOnCell[grid[row, col].objectInCellCounter - 1])
                        {
                            if (grid[row, col + 1].objectInCellCounter != 0 && grid[row, col + 1].ingredientsOnCell[grid[row, col + 1].objectInCellCounter - 1] == destinationIngredient)
                            {
                                newIngredientPosition = new Vector3(grid[row, col + 1].Center.x, 0.05f + 0.1f * grid[row, col + 1].objectInCellCounter, grid[row, col + 1].Center.z);
                                rowIndex = row;
                                colIndex = col;
                            }
                            else
                            {
                                selectedIngredient = null;
                                destinationIngredient = null;
                                movePiece = false;
                                movingIngredient = false;
                                return;
                            }

                        }
                    }
                }
            }
            grid[rowIndex, colIndex].objectInCellCounter--;
            grid[rowIndex, colIndex + 1].objectInCellCounter++;
            grid[rowIndex, colIndex + 1].ingredientsOnCell.Add(selectedIngredient);
            grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient);
            if (selectedIngredient.transform.childCount != 0)
            {
                for (int i = selectedIngredient.transform.childCount - 1; i >= 0; i--)
                {
                    if (selectedIngredient.transform.GetChild(i).transform.CompareTag("Ingredient"))
                    {
                        grid[rowIndex, colIndex].objectInCellCounter--;
                        grid[rowIndex, colIndex + 1].objectInCellCounter++;
                        grid[rowIndex, colIndex + 1].ingredientsOnCell.Add(selectedIngredient.transform.GetChild(i).gameObject);
                        grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient.transform.GetChild(i).gameObject);
                    }
                }
            }

            destination = grid[rowIndex, colIndex].rightBound;
            actualRotation = new Vector3(0, 0, -180);
            finalRotationDir = new Vector3(selectedIngredient.transform.eulerAngles.x, selectedIngredient.transform.eulerAngles.y, selectedIngredient.transform.eulerAngles.z - 180);
            colIndex = colIndex + 1;
            movePiece = true;

        }
        //move left
        else if (Mathf.RoundToInt(direction.x) == -1)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (grid[row, col].objectInCellCounter - 1 >= 0)
                    {
                        if (selectedIngredient == grid[row, col].ingredientsOnCell[grid[row, col].objectInCellCounter - 1])
                        {
                            if (grid[row, col - 1].objectInCellCounter != 0 && grid[row, col - 1].ingredientsOnCell[grid[row, col - 1].objectInCellCounter - 1] == destinationIngredient)
                            {
                                newIngredientPosition = new Vector3(grid[row, col - 1].Center.x, 0.05f + 0.1f * grid[row, col - 1].objectInCellCounter, grid[row, col - 1].Center.z);
                                rowIndex = row;
                                colIndex = col;
                            }
                            else
                            {
                                selectedIngredient = null;
                                destinationIngredient = null;
                                movePiece = false;
                                movingIngredient = false;
                                return;
                            }
                        }
                    }
                }
            }
            grid[rowIndex, colIndex].objectInCellCounter--;
            grid[rowIndex, colIndex - 1].objectInCellCounter++;
            grid[rowIndex, colIndex - 1].ingredientsOnCell.Add(selectedIngredient);
            grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient);
            if (selectedIngredient.transform.childCount != 0)
            {
                for (int i = selectedIngredient.transform.childCount - 1; i >= 0; i--)
                {
                    if (selectedIngredient.transform.GetChild(i).transform.CompareTag("Ingredient"))
                    {
                        grid[rowIndex, colIndex].objectInCellCounter--;
                        grid[rowIndex, colIndex - 1].objectInCellCounter++;
                        grid[rowIndex, colIndex - 1].ingredientsOnCell.Add(selectedIngredient.transform.GetChild(i).gameObject);
                        grid[rowIndex, colIndex].ingredientsOnCell.Remove(selectedIngredient.transform.GetChild(i).gameObject);
                    }
                }
            }

            destination = grid[rowIndex, colIndex].leftBound;
            actualRotation = new Vector3(0, 0, 180);
            finalRotationDir = new Vector3(selectedIngredient.transform.eulerAngles.x, selectedIngredient.transform.eulerAngles.y, selectedIngredient.transform.eulerAngles.z + 180);
            colIndex = colIndex - 1;
            movePiece = true;
        }
    }

    private void MovePiece()
    {

        if (selectedIngredient.transform.position != newIngredientPosition && !rotDone)
        {
            selectedIngredient.transform.RotateAround(destination, actualRotation, angle * Time.deltaTime);

        }
        else if (selectedIngredient.transform.position != newIngredientPosition && rotDone)
        {
            selectedIngredient.transform.rotation = Quaternion.RotateTowards(selectedIngredient.transform.rotation, Quaternion.Euler(finalRotationDir), (200 * grid[rowIndex, colIndex].objectInCellCounter) * Time.deltaTime);
            selectedIngredient.transform.position = Vector3.MoveTowards(selectedIngredient.transform.position, newIngredientPosition, movementSpeed * Time.deltaTime);
        }
        else
        {
            SetNewParent(rowIndex, colIndex);
            CheckForVictory();
            rowIndex = 0;
            colIndex = 0;
            selectedIngredient = null;
            destinationIngredient = null;
            debugUI.text = "";
            rotDone = false;
            movePiece = false;
            movingIngredient = false;
        }
    }

    private void SetNewParent(int row, int col)
    {
        for (int i = 0; i < grid[row, col].objectInCellCounter; i++)
        {
            grid[row, colIndex].ingredientsOnCell[i].transform.parent = null;
        }

        for (int i = 0; i < grid[row, col].objectInCellCounter - 1; i++)
        {
            grid[row, colIndex].ingredientsOnCell[i].transform.parent = grid[row, col].ingredientsOnCell[grid[row, col].objectInCellCounter - 1].transform;
        }
    }

    private void CheckForVictory()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (grid[row, col].objectInCellCounter != 0 && !otherCellsWithIngredient && !victoryCondition)
                {
                    //check if all ingredients are all in 1 cell
                    rowIndex = row;
                    colIndex = col;
                    victoryCondition = true;
                }
                if (grid[row, col].objectInCellCounter != 0 && victoryCondition && !(rowIndex == row && colIndex == col))
                {
                    //if there are multiple cells with ingredients, you lost
                    otherCellsWithIngredient = true;
                    victoryCondition = false;
                }
            }
        }

        if (victoryCondition)
        {
            if (grid[rowIndex, colIndex].ingredientsOnCell[0].gameObject.name == "Bread(Clone)" && grid[rowIndex, colIndex].ingredientsOnCell[grid[rowIndex, colIndex].objectInCellCounter - 1].gameObject.name == "Bread(Clone)")
            {
                UIManager.instance.victoryUI.gameObject.SetActive(true);
                victoryUI.text = "YOU WIN";
                victoryAchieved = true;
                Time.timeScale = 0;
            }
        }
        else
        {
            otherCellsWithIngredient = false;
        }
    }

    public void NewRandomLevel()
    {
        Time.timeScale = 1;
        startGame = false;
        IngredientController[] ingredients = FindObjectsOfType<IngredientController>();
        for (int i = 0; i < ingredients.Length; i++)
        {
            Destroy(ingredients[i].gameObject);
        }
        breadCounter = 0;
        GenerateRandomLevel();
        UIManager.instance.startGame.SetActive(true);
        UIManager.instance.reloadLevel.SetActive(false);
        UIManager.instance.loadLevel.SetActive(true);
        UIManager.instance.loadInputField.gameObject.SetActive(true);
        UIManager.instance.saveLevel.SetActive(true);
        UIManager.instance.saveInputField.gameObject.SetActive(true);
        UIManager.instance.newLevel.SetActive(true);
        UIManager.instance.plusBtn.SetActive(true);
        UIManager.instance.minusBtn.SetActive(true);
    }

    public void ReloadLevel()
    {
        Time.timeScale = 1;
        victoryAchieved = false;
        selectedIngredient = null;
        rowIndex = 0;
        colIndex = 0;
        selectedIngredient = null;
        destinationIngredient = null;
        debugUI.text = "";
        rotDone = false;
        movePiece = false;
        movingIngredient = false;
        startGame = false;
        SaveLevel(0);
        LoadLevel(0);
        GenerateDefaultLevel();
        UIManager.instance.startGame.SetActive(true);
        UIManager.instance.reloadLevel.SetActive(false);
        UIManager.instance.loadLevel.SetActive(true);
        UIManager.instance.loadInputField.gameObject.SetActive(true);
        UIManager.instance.saveLevel.SetActive(true);
        UIManager.instance.saveInputField.gameObject.SetActive(true);
        UIManager.instance.newLevel.SetActive(true);
        UIManager.instance.plusBtn.SetActive(true);
        UIManager.instance.minusBtn.SetActive(true);
        UIManager.instance.victoryUI.gameObject.SetActive(false);

    }

    public void LoadLevelOfIndex()
    {
        Time.timeScale = 1;
        startGame = false;
        GenerateDefaultLevel();
        UIManager.instance.startGame.SetActive(true);
        UIManager.instance.reloadLevel.SetActive(false);
    }

    public DataSaver CreateSaveData()
    {
        DataSaver save = new DataSaver();

        for (int i = 0; i < save.choosedDisposition.Length; i++)
        {
            save.choosedDisposition[i] = chooseIngredientsPosition[i];
        }

        return save;
    }

    public void SaveLevel()
    {
        string index = UIManager.instance.saveInputField.text;
        DataSaver save = CreateSaveData();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/level" + index + ".save");
        bf.Serialize(file, save);
        file.Close();

        Debug.Log("SavedLevel on index " + index);
    }

    public void LoadLevel()
    {
        string index = UIManager.instance.loadInputField.text;
        if (File.Exists(Application.persistentDataPath + "/level" + index + ".save"))
        {
            IngredientController[] ingredients = FindObjectsOfType<IngredientController>();
            for (int i = 0; i < ingredients.Length; i++)
            {
                Destroy(ingredients[i].gameObject);
            }

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/level" + index + ".save", FileMode.Open);
            DataSaver save = (DataSaver)bf.Deserialize(file);
            file.Close();

            chooseIngredientsPosition = new Ingredients[16];
            for (int i = 0; i < chooseIngredientsPosition.Length; i++)
            {
                chooseIngredientsPosition[i] = save.choosedDisposition[i];
            }

            Debug.Log("LoadedLevel of index " + index);
            LoadLevelOfIndex();
        }
        else
        {
            UIManager.instance.ShowDebugUI("Level of index " + index + " does not extst");
        }
    }

    public void SaveLevel(int index)
    {
        DataSaver save = CreateSaveData();

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/level" + index + ".save");
        bf.Serialize(file, save);
        file.Close();

        Debug.Log("SavedLevel");
    }

    public void LoadLevel(int index)
    {
        if (File.Exists(Application.persistentDataPath + "/level" + index + ".save"))
        {
            IngredientController[] ingredients = FindObjectsOfType<IngredientController>();
            for (int i = 0; i < ingredients.Length; i++)
            {
                Destroy(ingredients[i].gameObject);
            }

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/level" + index + ".save", FileMode.Open);
            DataSaver save = (DataSaver)bf.Deserialize(file);
            file.Close();

            chooseIngredientsPosition = new Ingredients[16];
            for (int i = 0; i < chooseIngredientsPosition.Length; i++)
            {
                chooseIngredientsPosition[i] = save.choosedDisposition[i];
            }

            Debug.Log("LoadedLevel");
        }
        else
        {
            UIManager.instance.ShowDebugUI("Level of index " + index + " does not extst");
        }
    }

    public void IncreaseDiff()
    {
        userDifficultyLevel++;
        if (userDifficultyLevel > 16)
        {
            userDifficultyLevel = 16;
            UIManager.instance.ShowDebugUI("No more than 16 pieces allowed");
        }
        UIManager.instance.difficoultyText.text = userDifficultyLevel.ToString();
    }

    public void DecreaseDiff()
    {
        userDifficultyLevel--;
        if (userDifficultyLevel < 4)
        {
            userDifficultyLevel = 4;
            UIManager.instance.ShowDebugUI("No less than 4 pieces allowed");
        }
        UIManager.instance.difficoultyText.text = userDifficultyLevel.ToString();
    }

    public bool ExistSameLevel(int index)
    {
        if (File.Exists(Application.persistentDataPath + "/level" + index + ".save"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
