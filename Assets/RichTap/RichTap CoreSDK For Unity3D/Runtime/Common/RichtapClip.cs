using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RichTap.Common 
{
    public class RichtapClip : ScriptableObject
    {

        [SerializeField]
        private string content;

        [SerializeField]
        public string clipName;


        public RichtapClip(string path)
        {
            Load(path);   
        }

        public void Load(string path)
        {
            content = File.ReadAllText(path);
            clipName = Path.GetFileName(path);
        }

        public string GetContent() => content;
        public string GetClipName() => clipName;

        public void SetContent(string newContent)
        {
            content = newContent;
        }
    }

}
