using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerLoss : MonoBehaviour
{
    private float _timer = 0f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            _timer += Time.deltaTime;
            if (_timer > GameManager.instance.TimeTillGameOver)
            {
                GameManager.instance.GameOver();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7)
        {
            _timer = 0f;
        }
    }
}
