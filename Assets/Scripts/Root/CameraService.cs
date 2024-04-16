using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace ProjectJetSetRadio
{
    public class CameraService
    {
        public Camera MainCam { get; private set; }
        public void Init()
        {
            MainCam = Camera.main;
        }


        public void SetMouseCursorState(bool state)
        {
            Cursor.lockState = !state ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = state;
        }
    }
}
