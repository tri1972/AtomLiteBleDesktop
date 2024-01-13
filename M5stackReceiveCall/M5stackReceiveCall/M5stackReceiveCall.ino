#include <BLEDevice.h>
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

void canvasPrint(int x,int y,int size,char * str){
      canvas.setTextSize(size);            // 文字倍率変更
      //canvasErace(x,y,size,strlen(str));
      canvas.setCursor(x, y);
      canvas.print(str);
}

void writeMessageBox(String str){
  char Buf[255];
  
  uint16_t top=90;
  uint16_t left=0;
  uint16_t bottom=210;
  uint16_t right=320;
  uint8_t size=16;//フォントサイズ(8,10,12,14,16,20,24)

  uint8_t rowNum=(uint8_t)((right-left)/size);
  uint8_t strLength=(uint8_t)str.length()/3;//UTF-8が3byteのため
  uint8_t startStr=0;
  uint8_t stopStr=rowNum;
  uint8_t endStr=0;
  String rowStr;
  
  Serial.println(str);
  Serial.println("Start writing");
  do{
    rowStr=str.substring(startStr*3,stopStr*3);
    canvas.print(rowStr);
    /*//日本語表示用
    rowStr.toCharArray(Buf, rowNum*3);
    fontDump(left,top+currentRow*size,Buf,size);
    */
    startStr+=rowNum;
    stopStr+=rowNum;
    currentRow++;
  }while(stopStr<=strLength);
  //ラストの一行を表示する
  endStr=(stopStr-rowNum)+(strLength%rowNum);
  if(endStr>0){
    rowStr=str.substring(startStr*3,endStr*3);
    canvas.print(rowStr);
    /*//日本語表示用
    rowStr.toCharArray(Buf, rowNum*3);
    fontDump(left,top+currentRow*size,Buf,size);
    */
    currentRow++;
  }else{
    currentRow--;
  }
}
class MyAdvertisedDeviceCallbacks : public BLEAdvertisedDeviceCallbacks
{
  void onResult(BLEAdvertisedDevice advertisedDevice)
  {
    Serial.printf("Advertised Device: %s \n", advertisedDevice.toString().c_str());
    writeMessageBox(advertisedDevice.toString().c_str());

    if(advertisedDevice.getName()==SERVER_NAME){
      Serial.println("Find Device!");
      canvas.print("Find Device!");
      Serial.println(advertisedDevice.getAddress().toString().c_str());
      advertisedDevice.getScan()->stop();
      pServerAddress = new BLEAddress(advertisedDevice.getAddress());
      doConnect = true;
    }

    /*
    if (advertisedDevice.haveServiceUUID() && advertisedDevice.isAdvertisingService(serviceUUID))
    {
      Serial.println("Device found!");
      advertisedDevice.getScan()->stop();
    }
    */
  }
};

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
    pushButtonServer=!pushButtonServer;
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
  canvas.setColorDepth(8);                             // CORE2 GRAY のスプライトは16bit以上で表示されないため8bitに設定
                              /*
  canvas.setTextWrap(false);  // 改行をしない（画面をはみ出す時自動改行する場合はtrue）
  canvas.setTextSize(1);      // 文字サイズ（倍率）
  */
  canvas.createSprite(lcd.width(), lcd.height());
  canvas.setTextFont(4); // フォントの指定
  canvas.setCursor(10, 120); // カーソル位置の指定
  canvas.pushSprite(0, 0);  // メモリ内に描画したcanvasを座標を指定して表示する

  M5.Speaker.setVolume(10);

  // setupで単発実行。繰り返し実行するならloopに配置する必要がある
  scan(); 
}

void loop()
{
  M5.update();  // ボタン状態更新
  canvas.setTextSize(1);            // 文字倍率変更
  if (doConnect == true) {
    delay(1 * 1000); 
    if (connectToServer(*pServerAddress)) {
      Serial.println("connected!");
        canvasPrint(80, 57,1,"connected!");
        canvas.setTextSize(1); 
      connected = true;
    } else {
    Serial.println("We have failed to connect to the server.");
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
    M5.Speaker.tone(659, 200);
  }else{
    M5.Speaker.mute();
  }

  canvas.pushSprite(0, 0);
}