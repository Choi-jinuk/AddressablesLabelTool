using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressablesLabelTool : EditorWindow
{
    private string folderPath = "Assets/AddressableAssets"; // 기본 폴더 경로
    private string labelName = "AutoLabel"; // 기본 라벨 이름
    private string groupName = "Default";   //기본 그룹 이름
    
    private string saveFolderPath = "Assets/Resources_moved";  // 기본 저장 경로
    private string fileName = "AssetLabelList.json";  // 저장할 파일 이름

    private AddressableAssetSettings settings;
    private List<string> groups = new List<string>();
    
    [MenuItem("Undead Tool/Addressable Label Auto Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AddressablesLabelTool>("AddressablesLabelTool");
    }

    private void OnEnable()
    {
        settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            RefreshGroups();
        }
    }
    
    private void RefreshGroups()
    {
        groups.Clear();
        foreach (var group in settings.groups)
        {
            if (group != null)
            {
                groups.Add(group.Name);
            }
        }
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Addressable Label Auto Assigner", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        labelName = EditorGUILayout.TextField("Label Name", labelName);

        if (groups.Count > 0)
        {
            int selectedGroupIndex = Mathf.Max(groups.IndexOf(groupName), 0);
            selectedGroupIndex = EditorGUILayout.Popup(selectedGroupIndex, groups.ToArray());
            groupName = groups[selectedGroupIndex];
        }
        else
        {
            GUILayout.Label("No Addressable Groups found", EditorStyles.label);
        }
        
        if (GUILayout.Button("Assign Addressable Labels"))
        {
            AssignLabelsToAssets(folderPath, labelName);
        }
        
        GUILayout.Label("Addressable Label Exporter", EditorStyles.boldLabel);
        GUILayout.Label("asset_ 으로 지정된 label 값만 저장됩니다. (CDN Download Check 용)", EditorStyles.label);
        
        GUILayout.Label("Save Folder Path:", EditorStyles.label);
        saveFolderPath = EditorGUILayout.TextField(saveFolderPath);
        GUILayout.Label("File Name:", EditorStyles.label);
        fileName = EditorGUILayout.TextField(fileName);
        
        if (GUILayout.Button("Export Addressable Labels"))
        {
            ExportLabelsToJson();
        }
    }

    private void AssignLabelsToAssets(string folderPath, string labelName)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is not selected.");
            return;
        }

        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError("No Addressable Group selected.");
            return;
        }

        if (string.IsNullOrEmpty(labelName))
        {
            Debug.LogError("No Addressable Label selected.");
            return;
        }
        
        // 선택된 폴더 내의 모든 프리팹 파일을 검색
        SetLabel(settings, AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:AnimatorController", new[] { folderPath }));
        
        SetLabel(settings, AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:AudioMixer", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Material", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Model", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Mesh", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Prefab", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Scene", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Shader", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Sprite", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:Texture", new[] { folderPath }));
        
        SetLabel(settings, AssetDatabase.FindAssets("t:TextAsset", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { folderPath }));
        SetLabel(settings, AssetDatabase.FindAssets("t:SpineAtlasAsset", new[] { folderPath }));
        
        
        // Addressable 설정 저장
        AssetDatabase.SaveAssets();
        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
        Debug.Log("All addressable assets have been updated with the label.");
    }

    void SetLabel(AddressableAssetSettings settings, string[] guids)
    {
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            Debug.LogError("No Addressable Group Valid.");
            return;
        }
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            // 프리팹이 Addressable로 설정되어 있지 않다면 패스
            if (entry == null)
            {
                continue;
            }

            entry.parentGroup = group;
            // 라벨 추가
            if (!entry.labels.Contains(labelName))
            {
                entry.SetLabel(labelName, true, true);
                Debug.Log($"Label '{labelName}' has been assigned to {assetPath}");
            }
            
            // 주소값을 간소화하여 설정 (예: 파일 이름만 사용)
            string simplifiedAddress = SimplifyAddress(assetPath);
            entry.address = simplifiedAddress;
        }
    }
    // 주소값을 단순화하는 함수
    private string SimplifyAddress(string assetPath)
    {
        // 예시: 경로에서 파일 이름만 추출하여 주소로 사용
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        return fileName;
    }
    
    
    private void ExportLabelsToJson()
    {
        // 어드레서블 세팅 가져오기
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found.");
            return;
        }

        // 라벨 목록 가져오기
        HashSet<string> allLabels = new HashSet<string>();
        foreach (var group in settings.groups)
        {
            foreach (var entry in group.entries)
            {
                foreach (var label in entry.labels)
                {
                    if (label.Contains("asset_") == false)
                        continue;
                    
                    allLabels.Add(label);  // 중복되지 않도록 HashSet에 추가
                }
            }
        }

        // 라벨을 JSON 배열로 변환
        string jsonArray = JsonUtility.ToJson(new LabelList(allLabels.ToList()), true);

        // 폴더가 없으면 생성
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        // 파일 경로 설정
        string fullPath = Path.Combine(saveFolderPath, fileName);

        // 파일 쓰기
        File.WriteAllText(fullPath, jsonArray);

        // 에셋 데이터베이스 갱신
        AssetDatabase.Refresh();

        Debug.Log($"Labels exported to {fullPath}");
    }

    [System.Serializable]
    public class LabelList
    {
        public List<string> labels;

        public LabelList(List<string> labels)
        {
            this.labels = labels;
        }
    }
}
