# AutoMouse
AutoGUI : Mouse &amp; keyboard script tool with image match template

指令說明 
---------------
* &#043; 之後的文字為選項 可加可不加<br>
* 有提供圖形偵測，如PIMG滑鼠會移動到符合的圖形內其中一點。<br>
* Image 為圖形名稱，螢幕截圖取出圖形後命名並放到data資料夾裡。<br>
* time 做完後要延遲的時間，單位秒，支援小數如1.2 = 1200ms<br>

指令如下 : 
---------------
* LOOP        : times //腳本執行次數，沒限制的情況下為無限
* Time        : time //單次腳本最大執行時間
* P + FA		  : x1 y1 x2 y2 time FA //移動滑鼠單次點擊，範圍為矩形內，點位為矩形左上跟右下
* PD +FA      : x1 y1 x2 y2 time FA //移動滑鼠雙擊，範圍為矩形內，點位為矩形左上跟右下
* PIMG + FA	  : Image pretime time FA //直到Image出現後，移動滑鼠到Image單次點擊。
* PIMGD + FA  : Image pretime time FA //直到Image出現後，移動滑鼠到Image單次雙擊。
* PIMG2		    : Image1 Image2 time //Image1 內的 Image2為目標
* PIMGP		    : Image x1 y1 x2 y2 time //Image 左上點與矩形做偏移後為目標
* CHECK		    : Image //確認Image有無出現有的話往下執行至END為止
* END         : //繼續往下執行
* END BREAK   : //結束本次腳本，進行下一次
* END STOP    : //結束整個腳本
* KEY			    : Key time //Key 要按的按鍵
* Until       : Image //直到Image出現才往下執行




