//#include <BLEDevice.h>
#include <NimBLEDevice.h>
//#include <BLEServer.h>
//#include <BLEUtils.h>
//#include <BLE2902.h>
#include <M5Stack.h>
#define LGFX_AUTODETECT // 自動認識(D-duino-32 XS, PyBadgeはパネルID読取れないため自動認識の対象から外れているそうです)
#define LGFX_USE_V1     // v1.0.0を有効に(v0からの移行期間の特別措置とのこと。書かない場合は旧v0系で動作)
#include <LovyanGFX.hpp>
#include <LGFX_AUTODETECT.hpp> // クラス"LGFX"を用意します
// #include <lgfx_user/LGFX_ESP32_sample.hpp> // またはユーザ自身が用意したLGFXクラスを準備します

#define SERVICE_UUID "e72609f6-2bcb-4fb0-824a-5276ec9e355d"
#define CHARACTERISTIC_UUID "cca99442-dab6-4f69-8bc2-685e2412d178"
#define SERVER_NAME         "ESP32PIRTRI"

#define RANGE_SCROLL_X 0
#define RANGE_SCROLL_Y 32
#define FONT_MAGNIFICATION 1

static BLEUUID serviceUUID(SERVICE_UUID);
static BLEUUID charUUID(CHARACTERISTIC_UUID);

static BLEAddress *pServerAddress;
static boolean doConnect = false;
static boolean connected = false;
static boolean pushButtonServer=false;
static BLERemoteCharacteristic* pRemoteCharacteristic;
//static boolean isInitBLEClient=false;
static BLEClient*  pClient = NULL;

static LGFX lcd; // LGFXのインスタンスを作成。
static LGFX_Sprite canvas(&lcd);  // スプライトを使う場合はLGFX_Spriteのインスタンスを作成
uint8_t currentRow=0; 

hw_timer_t * timer = NULL;
portMUX_TYPE timerMux = portMUX_INITIALIZER_UNLOCKED;
volatile uint32_t current_time = 0;

volatile static bool IsKeepAliveScan=false;
//static bool IsKeepAlive=false;
int IsKeepAliveDetectCount=false;
volatile uint32_t IsKeepAliveCount = 0;
bool IsKeepAliveDetect=false;
bool IsKeepAliveDetectBefore=false;

void lcdPrintln(char * str){      
  lcd.startWrite();
  lcd.println(str);
  lcd.endWrite();
  //lcd.display();
}

void lcdPrint(char * str,int x,int y){
  lcd.startWrite();
  lcd.setCursor(x, y);
  lcd.print(str);
  lcd.endWrite();
  //lcd.display();
}

void canvasPrint(int x,int y,int size,char * str){
      canvas.setTextSize(size);            // 文字倍率変更
      //canvasErace(x,y,size,strlen(str));
      canvas.setCursor(x, y);
      canvas.print(str);
}

/*
  スクロール外に文字列描画
  x:文字開始位置x
  y:文字開始位置y
  rect_x：文字数範囲x（全角文字(最大 26))
  rect_x：文字数範囲y
  text：文字列
*/
void lcdPrintFix(int x,int y,int rect_x,int rect_y,String text,int color){
  float maxStrNumRow=320/(FONT_MAGNIFICATION*16*0.75);
  // setScrollRectの範囲指定を解除します。
  lcd.clearScrollRect();
  lcd.startWrite();
  lcd.setTextColor(color,TFT_BLACK);
  lcd.setTextSize(FONT_MAGNIFICATION);            // 文字倍率変更
  lcd.setCursor(x*16, y*16);
  for(int i=0 ;i< (int)(rect_x*2/FONT_MAGNIFICATION);i++){//全角文字列数で計算
    lcd.print(" ");
  }
  //lcd.display();
  lcdPrint((char *)text.c_str(),x*16,y*16);
  lcd.endWrite();
  lcd.setTextSize(1);            // 文字倍率変更
  lcd.setScrollRect(RANGE_SCROLL_X , RANGE_SCROLL_Y , lcd.width() , lcd.height() );
  lcd.setTextColor(TFT_WHITE,TFT_BLACK);
  lcd.setCursor(0, 0);
}

class funcClientCallbacks: public BLEClientCallbacks {
    void onConnect(BLEClient* pClient) {
        Serial.println("onConnect Execute");
    };
    void onDisconnect(BLEClient* pClient) {
        connected = false;
        Serial.println("onDisconnect Execute");
    }
};

class MyAdvertisedDeviceCallbacks : public BLEAdvertisedDeviceCallbacks
{
  void onResult(BLEAdvertisedDevice *advertisedDevice)
  {
    //Serial.printf("Advertised Device: %s \n", advertisedDevice->toString().c_str());
    
    lcd.startWrite();
    lcd.printf("Advertised Device: %s \n", advertisedDevice->toString().c_str());
    lcd.endWrite();
    //lcd.display();

    if(advertisedDevice->getName()==SERVER_NAME){
      lcd.startWrite();
      
      //Serial.println("Find Device!");
      lcd.println("Find Device!");
      //Serial.println(advertisedDevice->getAddress().toString().c_str());
      lcd.println(advertisedDevice->getAddress().toString().c_str());
      advertisedDevice->getScan()->stop();
      pServerAddress = new BLEAddress(advertisedDevice->getAddress());
      doConnect = true;
      
      lcd.endWrite();
      //lcd.display();
    }
  }
};

void onTimer(){
  
  portENTER_CRITICAL_ISR(&timerMux);
  IsKeepAliveCount++;
  IsKeepAliveScan=true;
  portEXIT_CRITICAL_ISR(&timerMux);
  //current_time = millis();
  //Serial.printf("No. %u, %u ms\n", counter, current_time);
}

void scan()
{
  BLEScan *pBLEScan = BLEDevice::getScan();
  pBLEScan->setAdvertisedDeviceCallbacks(new MyAdvertisedDeviceCallbacks());
  // Interval, Windowはdefaultの値で動作して問題なさそうなため設定しない。
  // アドバタイズを受信するだけのためパッシブスキャン
  // trueにすると高速にペリフェラルを検出できるかもしれないが、パッシブでもすぐ検出できるため必要性は感じていない
  // https://github.com/espressif/arduino-esp32/blob/master/libraries/BLE/examples/BLE_scan/BLE_scan.ino#L27
  pBLEScan->setActiveScan(true);

  // スキャン5秒には特に意味はない。
  // スキャン結果を残しておく必要がないため、終わったクリアする。そのためにis_continueはfalseにする
  pBLEScan->start(5, false);
}


String converter(uint8_t *str){
    return String((char *)str);
}


static void notifyCallback(
  BLERemoteCharacteristic* pBLERemoteCharacteristic,  uint8_t* pData,  size_t length,  bool isNotify) {
    String receiveStr=converter(pData);
    Serial.println("Notify!!");
    Serial.printf("Server output:%s\n",receiveStr);
    pushButtonServer=true;
}

bool connectToServer(BLEAddress pAddress) {
    //Serial.print("Forming a connection to ");
    //Serial.println(pAddress.toString().c_str());

    pClient  = BLEDevice::createClient();
    pClient->setClientCallbacks(new funcClientCallbacks());
    pClient->connect(pAddress);
    //isInitBLEClient=true;

    BLERemoteService* pRemoteService = pClient->getService(serviceUUID); 
    //notify してクライアントの電源が切れると、サーバー側も電源を落とす。サービスが読み込めなくなるため。
    //Serial.println(pRemoteService->toString().c_str());
    if (pRemoteService == nullptr) {
      return false;
    }
    pRemoteCharacteristic = pRemoteService->getCharacteristic(charUUID);
    if (pRemoteCharacteristic == nullptr) {
      return false;
    }
    pRemoteCharacteristic->registerForNotify(notifyCallback);
    return true;
}

void disconnectToServer(){
  /*
  if(isInitBLEClient){
    pClient->disconnect();
    Serial.println("Disconnecting");
  }else{
    Serial.println("Already Disconnected");
  }
  */
  if(pClient!=NULL){
    pClient->disconnect();
    Serial.println("Disconnecting");
  }else{
    Serial.println("Already Disconnected");
  }
} 

void setup()
{
  M5.begin(); // M5 Coreの初期化
  BLEDevice::init("M5AtomLite BLE Client");

  Serial.begin(115200); // シリアル接続の開始
  delay(500);
 
  
  lcd.init();  
  lcd.setRotation(1);         // 画面向き設定（0～3で設定、4～7は反転)　※CORE2、GRAYの場合
  lcd.setTextColor(TFT_WHITE,TFT_BLACK);
  lcd.setFont(&fonts::lgfxJapanGothic_16);
  //lcdPrintFix(0,1,10,0,"こんにちは世界",TFT_GREEN);
  lcdPrintFix(0,1,10,0,"DEAD確認",TFT_RED);
  lcd.setCursor(0, 0);
  lcd.setTextScroll(true);
  lcd.setScrollRect(RANGE_SCROLL_X , RANGE_SCROLL_Y , lcd.width() , lcd.height() );
  lcd.setTextSize(FONT_MAGNIFICATION);            // 文字倍率変更

  M5.Speaker.setVolume(10);

  timer = timerBegin(0, 80, true);  // タイマ作成
  timerAttachInterrupt(timer, &onTimer, true);    // タイマ割り込みサービス・ルーチン onTimer を登録
  timerAlarmWrite(timer, 1000, true);  // 割り込みタイミング(us)の設定 = 1msでの設定
  timerAlarmEnable(timer);  // タイマ有効化
}

int i = 0;
String value="";

void loop()
{
  M5.update();  // ボタン状態更新

  if(!connected){//接続されていなければアドバタイジングされているデバイスをスキャンする    
    lcdPrintFix(0,0,10,0,"BLE切断",TFT_RED);
    scan(); 
  }else{
    if(M5.BtnC.isPressed()) {
      pushButtonServer=false;
    }
  }
  if (doConnect == true) {//デバイスが見つかれば接続動作にはいる
    delay(1 * 1000); 
    if (connectToServer(*pServerAddress)) {
      lcdPrintFix(0,0,10,0,"BLE接続",TFT_WHITE);
      connected = true;
    } else {
      lcdPrintln("We have failed to connect to the server.");
      connected = false;
    }
    doConnect = false;
  }
  
  if(Serial.available()){
    while (Serial.available()) {
      char data = Serial.read();
      if(data==0x61){
      //if(data==0x0a){
        IsKeepAliveDetectCount++;
        //Serial.println("SerialReceivedAliveDetect!!!!");
        //Serial.print(IsKeepAliveDetectCount, DEC);
        break;
      }
    }
    //Serial.end();
    //Serial.begin(115200);
  }
  
  //if(IsKeepAliveScan){
    if(IsKeepAliveCount>1000){
      //Serial.println("Alivecount!!!! 10");
      if(IsKeepAliveDetectCount>0){
        //lcdPrintFix(0,1,10,0,"ALIVE確認",TFT_WHITE);
        IsKeepAliveDetect=true;
        IsKeepAliveDetectCount=0;
        //Serial.println("AliveDetectTrue!!!!");
      }else{
        //lcdPrintFix(0,1,10,0,"DEAD確認",TFT_RED);
        IsKeepAliveDetect=false;
        //Serial.println("AliveDetectFalse!!!!");
      }
      portENTER_CRITICAL(&timerMux);
      IsKeepAliveCount=0;
      portEXIT_CRITICAL(&timerMux);
    }
    IsKeepAliveScan=false;
  //}

  if(IsKeepAliveDetect){//PC:off→on
    if(!IsKeepAliveDetectBefore){
      noInterrupts();
      lcdPrintFix(0,1,10,0,"ALIVE確認",TFT_WHITE);//BLE接続か接続中だとALIVE確認の表示がされない
      Serial.println("Alive!!!!");
      interrupts();
      disconnectToServer();
      IsKeepAliveDetectBefore=IsKeepAliveDetect;
    }
  }else{//PC:on→off
    if(IsKeepAliveDetectBefore){
      noInterrupts();
      Serial.println("Dead!!!!");
      connected=false;
      doConnect = false;
      lcdPrintFix(0,1,10,0,"DEAD確認",TFT_RED);
      IsKeepAliveDetectBefore=IsKeepAliveDetect;
      interrupts();
    }
  }

  if(pushButtonServer){
    lcdPrintFix(10,0,10,0,"呼び出し中",TFT_RED);
    //Serial.println("pushButtonServer true");
    M5.Speaker.tone(659, 200);
    delay(500);
    pushButtonServer=false;
  }else{
    lcdPrintFix(10,0,10,0,"",TFT_RED);
    //Serial.println("pushButtonServer false");
    M5.Speaker.mute();
  }
  lcd.display();
  //canvas.pushSprite(0, 0);
}