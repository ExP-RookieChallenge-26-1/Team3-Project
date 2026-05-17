using UnityEngine;

public interface ILaserHittable
{
    
        void OnLaserHit(); 
        /* damage는 적이나 다른 것들을 위해 만들어둠
         * brick에 들어갈 함수 예시
         * void OnLaserHit(float damage)
            {
                Destroy(gameObject);
            }
            즉 파괴만 되도록 하면 됨
        
         */

        
        
}
