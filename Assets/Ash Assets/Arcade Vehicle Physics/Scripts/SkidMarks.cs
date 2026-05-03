using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeVP
{
    public class SkidMarks : MonoBehaviour
    {
        [SerializeField] private TrailRenderer skidMark;
        [SerializeField] private TrailRenderer skidMark2;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem smoke2;
        public ArcadeVehicleController carController;
        float fadeOutSpeed;
        private void Awake()
        {
            skidMark.emitting = false;
            skidMark2.emitting = false;
            skidMark.startWidth = carController.skidWidth;
            skidMark2.startWidth = carController.skidWidth;
        }


        private void OnEnable()
        {
            skidMark.enabled = true;
            skidMark2.enabled = true;
        }
        private void OnDisable()
        {
            skidMark.enabled = false;
            skidMark2.enabled = false;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (carController.grounded())
            {

                if (Mathf.Abs(carController.carVelocity.x) > 10)
                {
                    fadeOutSpeed = 0f;
                    skidMark.materials[0].color = Color.black;
                    skidMark2.materials[0].color = Color.black;
                    skidMark.emitting = true;
                    skidMark2.emitting = true;
                }
                else
                {
                    skidMark.emitting = false;
                    skidMark2.emitting = false;
                }
            }
            else
            {
                skidMark.emitting = false;
                skidMark2.emitting = false;

            }
            if (!skidMark.emitting)
            {
                fadeOutSpeed += Time.deltaTime / 2;
                Color m_color = Color.Lerp(Color.black, new Color(0f, 0f, 0f, 0f), fadeOutSpeed);
                skidMark.materials[0].color = m_color;
                skidMark2.materials[0].color = m_color;
                if (fadeOutSpeed > 1)
                {
                    skidMark.Clear();
                    skidMark2.Clear();
                }
            }

            // smoke
            if (skidMark.emitting == true)
            {
                smoke.Play();
                smoke2.Play();
            }
            else
            {
                smoke.Stop();
                smoke2.Stop();
            }

        }
    }
}
