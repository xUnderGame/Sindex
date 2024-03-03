using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.SqliteClient;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SQLManager : MonoBehaviour
{
    [HideInInspector] public static SQLManager Instance;
    [HideInInspector] public int sinSelectedFromMenu;
    
    private string streamingPath = $"URI=file:{Application.streamingAssetsPath}/Database/Pecados.db";
    private string persistentPath;
    private IDbConnection connection;
    private IDbCommand command;
    private IDataReader reader;
    private Text outputText;

    void Start()
    {
        // Only one SQLManager
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Try get new references on scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
        sinSelectedFromMenu = 1;
        TryGetNewReferences();

        StartCoroutine(RunDbCode());

        // Query(
        //     "SELECT P.Pecado_ID, P.Nombre, P.Pecado, P.Descripcion, E.Nombre, E2.Nombre, P.Area, P.Fortaleza, P.Debilidad" +
        //     " FROM Pecados P JOIN Elementos E ON P.Elemento1 = E.Elemento_ID" +
        //     " JOIN Elementos E2 ON P.Elemento2 = E2.Elemento_ID" +
        //     " JOIN Areas A ON P.Area = A.Area_ID;"
        // );
    }

    private void TryGetNewReferences()
    {
        outputText = GameObject.Find("Output Text").GetComponent<Text>();
    }

    // Hacer una consulta
    public List<string> Query(string query = "SELECT * FROM Pecados")
    {
        // Create the command
        command = connection.CreateCommand();
        command.CommandText = query;

        // Close and open new reader
        reader?.Close();
        reader = command.ExecuteReader();

        // Read the contents
        List<string> data = new();
        if (outputText) outputText.text = string.Empty;

        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var result = reader.GetString(i);
                if (outputText) outputText.text += result + ", ";
                data.Add(result);
            }
        }

        return data;
    }

    // Input field
    public List<string> SearchInput(string input, bool useID = true)
    {
        if (input.Trim() == string.Empty) return null; 
        return Query($"SELECT * FROM Pecados P WHERE (P.Pecado_ID = '{input}' AND {useID} IS TRUE) OR P.Nombre LIKE  '%{input}%' OR (P.Area = '{input}' AND {useID} IS FALSE)");
    }

    // Gets new references once you change scenes
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryGetNewReferences();
    }

    public IEnumerator RunDbCode()
    {
        // Where to copy the db to
        string dbDestination = Path.Combine(Application.persistentDataPath, "Pecados.db");

        //Check if the File do not exist then copy it
        if (!File.Exists(dbDestination))
        {
            // Where the db file is at
            string dbStreamingAsset = Path.Combine(Application.streamingAssetsPath, "Pecados.db");

            byte[] result;

            // Read the File from streamingAssets. Use WWW for Android
            if (dbStreamingAsset.Contains("://") || dbStreamingAsset.Contains(":///"))
            {
                WWW www = new WWW(dbStreamingAsset); // No voy a cambiarlo a unitywebapi.
                yield return www;
                result = www.bytes;
            }
            else
            {
                result = File.ReadAllBytes(dbStreamingAsset);
            }

            // Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(dbDestination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbDestination));
            }

            // Copy the data to the persistentDataPath where the database API can freely access the file
            File.WriteAllBytes(dbDestination, result);
        }

        // Open DB connection
        dbDestination = "URI=file:" + dbDestination;

        connection = new SqliteConnection(dbDestination);
        connection.Open();

        // Default command
        Query(
            "SELECT P.Pecado_ID, P.Nombre, P.Pecado, P.Descripcion, E.Nombre, E2.Nombre, P.Area, P.Fortaleza, P.Debilidad" +
            " FROM Pecados P JOIN Elementos E ON P.Elemento1 = E.Elemento_ID" +
            " JOIN Elementos E2 ON P.Elemento2 = E2.Elemento_ID" +
            " JOIN Areas A ON P.Area = A.Area_ID;"
        );
    }
}
