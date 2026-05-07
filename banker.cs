// Author: Steve Phillips-Ward
// Course: CSC 360 Operating Systems
// Project 4: Banker's Algorithm
// Due Date: 2025-07-05
// Instructor: Dr. Siming Liu
// Description: This program simulates the Banker's Algorithm for deadlock avoidance. Written in C#, it reads input from a file containing the number of processes, resources, allocation matrix, maximum matrix, available vector, and a request vector. It checks if the system is in a safe state and whether a request can be granted without leading to an unsafe state.


using System;
using System.IO;
using System.Linq;

// Banker's Algorithm Implementation
//Main class for the Algorithm
class Banker
{
    // Global variables for allocation, max, need matrices, available vector, request vector, number of processes (n), number of resources (m), and the requesting process index
    static int[,] allocation, max, need;
    static int[] available, request;
    static int n, m, requestingProcess; //n = number of processes, m = number of resources types
    /*
    Main method to execute the Banker's Algorithm
    It reads input from a file specified as a command line argument, initializes matrices and vectors, checks if the system is in a safe state, and determines if a request can be granted.
    It prints the allocation, max, need matrices, available vector, and request vector to the console.
    */
    public static void Main(string[] args)
    {
        //Ensure exactly one argument is passed for the input filename
        if (args.Length != 1)
        {
            Console.WriteLine("Please provide the input filename as a command line argument.");
            return;
        }

        string[] lines = File.ReadAllLines(args[0]);
        int line = 0;

        // Helper method to read the next non-empty line
        //Returns the next non-empty trimmed line from input
        string ReadNextNonEmptyLine(string[] inputLines, ref int index)
        {
            while (index < inputLines.Length && string.IsNullOrWhiteSpace(inputLines[index]))
            {
                index++;
            }
            if (index >= inputLines.Length)
            {
                throw new Exception("Unexpected end of input file.");
            }
            return inputLines[index++].Trim();
        }

        //Read number of processes and resources
        n = int.Parse(ReadNextNonEmptyLine(lines, ref line));
        m = int.Parse(ReadNextNonEmptyLine(lines, ref line));
        // Initialize matrices and vectors based on the number of processes and resources
        allocation = new int[n, m];
        max = new int[n, m];
        need = new int[n, m];
        available = new int[m];
        //Read allocation matrix
        for (int i = 0; i < n; i++)
        {
            var tokens = ReadNextNonEmptyLine(lines, ref line)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToArray();
            for (int j = 0; j < m; j++)
            {
                allocation[i, j] = tokens[j];
            }
        }
        //Read max matrix
        for (int i = 0; i < n; i++)
        {
            var tokens = ReadNextNonEmptyLine(lines, ref line)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToArray();
            for (int j = 0; j < m; j++)
            {
                max[i, j] = tokens[j];
            }
        }

        // Read and parse available vector (skip possible label line)
        while (true)
        {
            var nextLine = ReadNextNonEmptyLine(lines, ref line);
            var availableTokens = nextLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (availableTokens.All(token => int.TryParse(token, out _)))
            {
                available = availableTokens.Select(int.Parse).ToArray();
                break;
            }
            
        }

        // Read and parse request line
        string requestLine = ReadNextNonEmptyLine(lines, ref line);
        int colonIndex = requestLine.IndexOf(':');
        if (colonIndex == -1)
        {
            throw new Exception("Request line must contain ':' separator.");
        }
        //Parse the requesting process and request vector
        string processPart = requestLine.Substring(0, colonIndex).Trim();
        string requestPart = requestLine.Substring(colonIndex + 1).Trim();
        requestingProcess = int.Parse(processPart);
        request = requestPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        //Echo the data
        Console.WriteLine($"There are {n} processes in the system.\n");
        Console.WriteLine($"There are {m} resources in the system.\n");
        //Display allocation and max matrices
        PrintMatrix("Allocation", allocation);
        PrintMatrix("Max", max);

        // Calculate Need Matrix
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                need[i, j] = max[i, j] - allocation[i, j];
            }
        }
        // Output the need and available
        PrintMatrix("Need", need);
        PrintVector("Available", available);
        // Check if the system is in a safe state
        Console.WriteLine(SafeState() ? "\nThe system is in a safe state!" : "\nThe system is not in a safe state!");
        //Output the request vector
        PrintRequest();
        // Check if the request can be granted
        if (CanGrantRequest()) 
        {
            Console.WriteLine("\nThe request can be granted.");

            // Temporarily apply the request to show updated Available Vector
            for (int j = 0; j < m; j++)
            {
                available[j] -= request[j];
                allocation[requestingProcess, j] += request[j];
                need[requestingProcess, j] -= request[j];
            }

            PrintVector("Available", available);

            // Roll back the temporary state
            for (int j = 0; j < m; j++)
            {
                available[j] += request[j];
                allocation[requestingProcess, j] -= request[j];
                need[requestingProcess, j] += request[j];
            }
        }
        else
        {
            Console.WriteLine("\nThe request cannot be granted.");
        }
    }
    /*
        PrintMatrix method to display matrices
        It prints the matrix with row and column headers for better readability.
        Value Parameters:
        - name: The name of the matrix to be printed.   String
        - matrix: The matrix to be printed.             int[,]

        Reference Parameters:
        - None

        Local Variable Data Dictionary:
        i,j : Loop counters for rows and columns of the matrix.     int
        c   : Character used for column headers.                    char

    */
    static void PrintMatrix(string name, int[,] matrix)
    {
        Console.WriteLine($"\nThe {name} Matrix is...");
        Console.Write("   ");
        for (char c = 'A'; c < 'A' + m; c++)
        {
            Console.Write(c + " ");
        }
        Console.WriteLine();
        for (int i = 0; i < n; i++)
        {
            Console.Write($"{i}: ");
            for (int j = 0; j < m; j++)
            {
                Console.Write(matrix[i, j] + " ");
            }
            Console.WriteLine();
        }
    }
    /*
        PrintVector 
        method to display vectors
        It prints the vector with a header for better readability.
        Value Parameters:
        - name: The name of the vector to be printed.   String
        - vector: The vector to be printed.             int[]

        Reference Parameters:
        - None

        Local Variable Data Dictionary:
        value : Loop counter for the vector elements.     int
    */
    static void PrintVector(string name, int[] vector)
    {
        Console.WriteLine($"\nThe {name} Vector is...");
        Console.Write("  ");
        for (char c = 'A'; c < 'A' + m; c++)
        {
            Console.Write(c + " ");
        }
        Console.WriteLine();
        Console.Write("  ");
        foreach (var value in vector)
        {
            Console.Write(value + " ");
        }
        Console.WriteLine();
    }
    /*
        PrintRequest 
        method to display the request vector
        It prints the request vector with a header for better readability.
        Value Parameters:
        - None

        Reference Parameters:
        - None

        Local Variable Data Dictionary:
        r current request value being printed.     int
    */
    static void PrintRequest()
    {
        Console.WriteLine($"\nThe Request Vector is...");
        Console.Write("  ");
        for (char c = 'A'; c < 'A' + m; c++)
        {
            Console.Write(c + " ");
        }
        Console.WriteLine();
        Console.Write($"{requestingProcess}: ");
        foreach (var r in request)
        {
            Console.Write(r + " ");
        }
        Console.WriteLine();
    }
    /*
        SafeState method to check if the system is in a safe state
        It simulates the allocation of resources and checks if all processes can finish.
        Return Value:
        - Returns true if the system is in a safe state, false otherwise.

        Value Parameters:
        - None

        Reference Parameters:
        - None

        Local Variable Data Dictionary:
        work : Temporary array to simulate available resources.   int[]
        finish : Array to track if processes can finish.          bool[]
        i, j : Loop counters for processes and resources.         int
    */
    static bool SafeState()
    {
        int[] work = (int[])available.Clone();
        bool[] finish = new bool[n];

        while (true)
        {
            bool progress = false;
            for (int i = 0; i < n; i++)
            {
                if (!finish[i])
                {
                    bool canFinish = true;
                    for (int j = 0; j < m; j++)
                    {
                        if (need[i, j] > work[j])
                        {
                            canFinish = false;
                            break;
                        }
                    }

                    if (canFinish)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            work[j] += allocation[i, j];
                        }
                        finish[i] = true;
                        progress = true;
                    }
                }
            }
            if (!progress)
            {
                break;
            }
        }

        return finish.All(f => f);
    }
    /*
        CanGrantRequest method to check if a request can be granted
        It checks if the request can be satisfied without leading to an unsafe state.
        Return Value:
        - Returns true if the request can be granted, false otherwise.

        Value Parameters:
        - None

        Reference Parameters:
        - None

        Local Variable Data Dictionary:
        j : Loop counter for resources.     int
        isSafe : Temporary variable to check if the system remains in a safe state after the request is granted.   bool
    */
    static bool CanGrantRequest()
    {
        for (int j = 0; j < m; j++)
        {
            if (request[j] > need[requestingProcess, j] || request[j] > available[j])
            {
                return false;
            }
        }

        // Temporarily allocate to test safety
        for (int j = 0; j < m; j++)
        {
            available[j] -= request[j];
            allocation[requestingProcess, j] += request[j];
            need[requestingProcess, j] -= request[j];
        }

        bool isSafe = SafeState();

        // Roll back
        for (int j = 0; j < m; j++)
        {
            available[j] += request[j];
            allocation[requestingProcess, j] -= request[j];
            need[requestingProcess, j] += request[j];
        }

        return isSafe;
    }
}
