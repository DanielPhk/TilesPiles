using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public enum Ingredients
{
    Empty,
    Bacon,
    Bread,
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


    private void Awake()
    {
        if(instance != null && instance != null)
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
        //grid setup
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
    void Update()
    {
        if (!victoryAchieved)
        {
            if (!movingIngredient)
            {
                DetectInput();
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

    public void ReloadLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

}
