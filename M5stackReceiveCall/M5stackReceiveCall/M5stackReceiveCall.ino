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

static BLEUUID serviceUUID(SERVICE_UUID);
static BLEUUID charUUID(CHARACTERISTIC_UUID);

static BLEAddress *pServerAddress;
static boolean doConnect = false;
static boolean connected = false;
static boolean pushButtonServer=false;
static BLERemoteCharacteristic* pRemoteCharacteristic;

static LGFX lcd; // LGFXのインスタンスを作成。
static LGFX_Sprite canvas(&lcd);  // スプライトを使う場合はLGFX_Spriteのインスタンスを作成
uint8_t currentRow=0; 

void lcdPrintln(char * str){      
  lcd.startWrite();
  lcd.println(str);
  lcd.endWrite();
  lcd.display();
}

void canvasPrint(int x,int y,int size,char * str){
      canvas.setTextSize(size);            // 文字倍率変更
      //canvasErace(x,y,size,strlen(str));
      canvas.setCursor(x, y);
      canvas.print(str);
}

class funcClientCallbacks: public BLEClientCallbacks {
    void onConnect(BLEClient* pClient) {
    };
    void onDisconnect(BLEClient* pClient) {
        connected = false;
    }
};

class MyAdvertisedDeviceCallbacks : public BLEAdvertisedDeviceCallbacks
{
  void onResult(BLEAdvertisedDevice *advertisedDevice)
  {
    Serial.printf("Advertised Device: %s \n", advertisedDevice->toString().c_str());
    
    lcd.startWrite();
    lcd.printf("Advertised Device: %s \n", advertisedDevice->toString().c_str());
    lcd.endWrite();
    lcd.display();

    if(advertisedDevice->getName()==SERVER_NAME){
      lcd.startWrite();
      
      Serial.println("Find Device!");
      lcd.println("Find Device!");
      Serial.println(advertisedDevice->getAddress().toString().c_str());
      lcd.println(advertisedDevice->getAddress().toString().c_str());
      advertisedDevice->getScan()->stop();
      pServerAddress = new BLEAddress(advertisedDevice->getAddress());
      doConnect = true;
      
      lcd.endWrite();
      lcd.display();
    }
  }
};
/*
class MyAdvertisedDeviceCallbacks : public BLEAdvertisedDeviceCallbacks
{
  void onResult(BLEAdvertisedDevice advertisedDevice)
  {
    Serial.printf("Advertised Device: %s \n", advertisedDevice.toString().c_str());
    //writeMessageBox(advertisedDevice.toString().c_str());
    
    lcd.startWrite();
    lcd.printf("Advertised Device: %s \n", advertisedDevice.toString().c_str());
    lcd.endWrite();
    lcd.display();

    if(advertisedDevice.getName()==SERVER_NAME){
      lcd.startWrite();
      
      Serial.println("Find Device!");
      lcd.println("Find Device!");
      Serial.println(advertisedDevice.getAddress().toString().c_str());
      lcd.println(advertisedDevice.getAddress().toString().c_str());
      advertisedDevice.getScan()->stop();
      pServerAddress = new BLEAddress(advertisedDevice.getAddress());
      doConnect = true;
      
      lcd.endWrite();
      lcd.display();
    }
  }
};
*/
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
    lcdPrintln("Server send data!");
    /*
    if(receiveStr.compareTo("PUSH_ON")){
      Serial.println("true");
      pushButtonServer=true;
    }else{
      Serial.println("false");
      pushButtonServer=false;
    }
    */
}

bool connectToServer(BLEAddress pAddress) {
    Serial.print("Forming a connection to ");
    Serial.println(pAddress.toString().c_str());

    BLEClient*  pClient  = BLEDevice::createClient();
    pClient->setClientCallbacks(new funcClientCallbacks());
    pClient->connect(pAddress);

    BLERemoteService* pRemoteService = pClient->getService(serviceUUID); 
    //notify してクライアントの電源が切れると、サーバー側も電源を落とす。サービスが読み込めなくなるため。
    Serial.println(pRemoteService->toString().c_str());
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

void setup()
{
  M5.begin(); // M5 Coreの初期化
  BLEDevice::init("M5AtomLite BLE Client");

  Serial.begin(115200); // シリアル接続の開始
  delay(500);
 
  
  lcd.init();  
  lcd.setRotation(1);         // 画面向き設定（0～3で設定、4～7は反転)　※CORE2、GRAYの場合
  /*
  canvas.setTextWrap(false);  // 改行をしない（画面をはみ出す時自動改行する場合はtrue）
  canvas.setTextSize(1);      // 文字サイズ（倍率）
  */
  lcd.setTextColor(TFT_BLACK, TFT_WHITE);

  lcd.setFont(&fonts::lgfxJapanGothic_16);
  lcdPrintln("HelloWorld");
  lcdPrintln("こんにちは世界");

  lcd.setCursor(0, 0);
  lcd.setTextScroll(true);
  lcd.setScrollRect(0 , 0 , lcd.width() , lcd.height() );
  lcd.setTextSize(1);            // 文字倍率変更

  /*//spriteで画面表示を行う箇所（スクロールさせるためとりあえずコメントアウト
  canvas.setColorDepth(8);                             // CORE2 GRAY のスプライトは16bit以上で表示されないため8bitに設定
  canvas.createSprite(lcd.width(), lcd.height());
  canvas.setTextFont(4); // フォントの指定
  canvas.setCursor(10, 120); // カーソル位置の指定
  canvas.pushSprite(0, 0);  // メモリ内に描画したcanvasを座標を指定して表示する
  */
  M5.Speaker.setVolume(10);
}
int i = 0;
void loop()
{
  M5.update();  // ボタン状態更新

  if(!connected){//接続されていなければアドバタイジングされているデバイスをスキャンする
    lcdPrintln("切断!! Scanning中...");
    scan(); 
  }

  if (doConnect == true) {//デバイスが見つかれば接続動作にはいる
    delay(1 * 1000); 
    if (connectToServer(*pServerAddress)) {
      lcdPrintln("接続!!");
      //lcdPrintln("connected!");
      connected = true;
    } else {
      lcdPrintln("We have failed to connect to the server.");
      connected = false;
    }
    doConnect = false;
  }

  
  if (connected) {
    if(M5.BtnC.isPressed()) {
      pushButtonServer=false;
    }
  }

  if(pushButtonServer){
    //Serial.println("pushButtonServer true");
    M5.Speaker.tone(659, 200);
    delay(500);
    pushButtonServer=false;
  }else{
    //Serial.println("pushButtonServer false");
    M5.Speaker.mute();
  }
  canvas.pushSprite(0, 0);
}