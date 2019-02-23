using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


public class InventoryModule : MonoBehaviour {
	public int previousItem;
	public int itemCount;
	public int selectedItem = 1;
    public GameObject player_class_obj;
    CPMovement player_class;
    public Vector3 vel;
    public Vector3 vel_raw;
    public float t;
    public float dt;
    public float dtx;
    public Transform gun_root;
    // Use this for initialization
    void Start () {

		SelectItem ();
	}

	void SelectItem()
	{
		//previousItem = selectedItem;
		int i = 0;
		foreach (Transform item in transform) {
			if (i == selectedItem) {
				item.gameObject.SetActive (true);
				previousItem = i;
			} else {
				item.gameObject.SetActive (false);
			}
			i++;
			itemCount = i - 1;
		}
	}

	// Update is called once per frame
	void Update () {


		if (Input.GetAxis("Mouse ScrollWheel") > 0) {
			selectedItem++;
			if (selectedItem > itemCount) {
				selectedItem = itemCount;
			}
		}
		if (Input.GetAxis ("Mouse ScrollWheel") < 0) {
			selectedItem--;
			if (selectedItem < itemCount) {
				selectedItem = 0;
			}
		}
		if (Input.GetKeyDown (KeyCode.Q)) {
			//selectedItem = previousItem;
			print ("Q");
		}

		if (selectedItem != previousItem) {
			SelectItem ();
		}
	}
}
