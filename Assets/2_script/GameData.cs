using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData 
{
   public static GameData Instance { get; } = new GameData();

   private List<Cargo> _deliveredCargos = new List<Cargo>();
   private List<string> _grades = new List<string> { "NOT BAD", "COOL", "OKAY","NICE","AWESOME" };
   private string _grade;
   
   private CarItemSO _carItem;

   public CarItemSO CarItem => _carItem;
   public string Grade => GetGrade();
   public string lastGrade => _grade;
   public  List<Sprite> Sprites => GetSprites();

   public void SetCar(CarItemSO carItem)
   {
      _carItem = carItem;
   }
   
   private string GetGrade()
   {
      string grade= _grades[0];
      
      if (_deliveredCargos.Count >= 200) grade = "AWESOME";
      else if (_deliveredCargos.Count >= 100)  grade = "NICE";
      else if (_deliveredCargos.Count >= 50)  grade = "COOL";
      else if (_deliveredCargos.Count >= 30)  grade = "OKAY";
      else if (_deliveredCargos.Count >= 10)  grade = "NOT BAD";
      
      _grade = grade;
      return grade;
   }

   private List<Sprite> GetSprites()
   {
      List<Sprite> sprites = new List<Sprite>();
      sprites.Clear();
      
      foreach (var sprite in _deliveredCargos)
      {
         sprites.Add(sprite.Icon);
      }
      
      return sprites;
   }

   public void AddCargo(Cargo cargo)
   {
      if (cargo == null)
         return;

      _deliveredCargos.Add(cargo);
   }

   public void ClearDeliveredCargos()
   {
      _deliveredCargos.Clear();
   }
}
