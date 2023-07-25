#include <BLEDevice.h>
#include <BLEServer.h>
//#include <BLEUtils.h>
#include <BLE2902.h>
#include <M5Stack.h>




#include <sdfonts.h>
#define SD_PN 4


/*定義部分*/
#define DEVICE_NAME "M5STACKTRI" //Bluetoothデバイス名設定
#define SERVICE_UUID        "ee007086-0dc9-4a48-b381-0f9e56d8c597"
#define CHARACTERISTIC_UUID "245c84dc-9422-41fb-bbf9-ddcd7da28120"
#define BLE_PASSKEY 123456 //ペアリング時のパスコード値設定

#define PIR_PIN   21  //PIR出力PINを指定

/*列挙型*/
enum statusBLEState{
  connect,
  disconnect,
  ASAP,
  WAIT,
  WRONG,
  EMERGENCY,
  GOOD,
  none,
};

/*グローバル変数*/
BLEServer* pServer = NULL;
BLECharacteristic* pCharacteristic = NULL;
volatile statusBLEState bleState=disconnect;
bool deviceConnected = false;
bool oldDeviceConnected = false;
bool beforeStateSwitch=LOW;

int volume = 1;


// フォントデータの表示
// buf(in) : フォント格納アドレス
// ビットパターン表示
// d: 8ビットパターンデータ
void fontDisp(uint16_t x, uint16_t y, uint8_t* buf) {
  uint32_t txt_color = TFT_WHITE;
  uint32_t bg_color = TFT_BLACK;

  uint8_t bn = SDfonts.getRowLength();               // 1行当たりのバイト数取得
  Serial.print(SDfonts.getWidth(), DEC);            // フォントの幅の取得
  Serial.print("x");
  Serial.print(SDfonts.getHeight(), DEC);           // フォントの高さの取得
  Serial.print(" ");
  Serial.println((uint16_t)SDfonts.getCode(), HEX); // 直前し処理したフォントのUTF16コード表示

  for (uint8_t i = 0; i < SDfonts.getLength(); i += bn ) {
    for (uint8_t j = 0; j < bn; j++) {
      for (uint8_t k = 0; k < 8; k++) {
        if (buf[i + j] & 0x80 >> k) {
          M5.Lcd.drawPixel(x + 8 * j + k , y + i / bn, txt_color);
        } else {
          M5.Lcd.drawPixel(x + 8 * j + k , y + i / bn, bg_color);
        }
      }
    }
  }
}


// 指定した文字列を指定したサイズで表示する
// pUTF8(in) UTF8文字列
// sz(in)    フォントサイズ(8,10,12,14,16,20,24)
void fontDump(uint16_t x, uint16_t y, char* pUTF8, uint8_t sz) {
  uint8_t buf[MAXFONTLEN]; // フォントデータ格納アドレス(最大24x24/8 = 72バイト)
  SDfonts.open();                                   // フォントのオープン
  SDfonts.setFontSize(sz);                          // フォントサイズの設定
  uint16_t mojisu = 0;
  while ( pUTF8 = SDfonts.getFontData(buf, pUTF8) ) { // フォントの取得
    fontDisp(x + mojisu * sz, y, buf);                 // フォントパターンの表示
    ++mojisu;
  }

  SDfonts.close();                                  // フォントのクローズ
}



/*コールバック関数用クラス*/

//Bleサーバー接続コールバック
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
      bleState=connect;
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
      bleState=disconnect;
    }
};

//BleCharacteristicコールバック
class MyCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      std::string value = pCharacteristic->getValue();
      String strValue=value.c_str();

      if (strValue.length() > 0) {
        if(strValue.equals("a")){
            Serial.println("ASAP");
            bleState=ASAP;
        }else if(strValue.equals("b")){
            Serial.println("WAIT");
            bleState=WAIT;
        }else if(strValue.equals("c")){
            Serial.println("WRONG");
            bleState=WRONG;
        }else if(strValue.equals("d")){
            Serial.println("CANCEL");
            bleState=connect;
        }else if(strValue.equals("e")){
            Serial.println("EMERGENCY");
            bleState=EMERGENCY;
        }else{
            bleState=none;
        }
        Serial.println("*********");
        Serial.print("New value: ");
        for (int i = 0; i < value.length(); i++)
          Serial.print(value[i]);

        Serial.println();
        Serial.println("*********");
      }
      
    }
};

// ペアリング処理用
//参考Url：https://qiita.com/poruruba/items/eff3fedb1d4a63cbe08d
class MySecurity : public BLESecurityCallbacks {
  bool onConfirmPIN(uint32_t pin){
    return false;
  }

  uint32_t onPassKeyRequest(){
    Serial.println("ONPassKeyRequest");
    return BLE_PASSKEY;
  }

  void onPassKeyNotify(uint32_t pass_key){
    // ペアリング時のPINの表示
    Serial.println("onPassKeyNotify number");
    Serial.println(pass_key);
  }

  bool onSecurityRequest(){
    Serial.println("onSecurityRequest");
    return true;
  }

  void onAuthenticationComplete(esp_ble_auth_cmpl_t cmpl){
    Serial.println("onAuthenticationComplete");
    if(cmpl.success){
      // ペアリング完了
      Serial.println("auth success");
    }else{
      // ペアリング失敗
      Serial.println("auth failed");
    }
  }
};

void setup() {
  // put your setup code here, to run once:
  M5.begin(); // M5 Coreの初期化
  M5.Power.begin(); // Power.moduleの初期化

  Serial.begin(115200); // シリアル接続の開始
  delay(500);

  //BLEデバイスの作成
  BLEDevice::init(DEVICE_NAME);
  //  BLE Serverの作成
  pServer = BLEDevice::createServer();
  // BLE Serviceの作成
  BLEService *pService = pServer->createService(SERVICE_UUID);
  // BLE Characteristicの作成
  pCharacteristic = pService->createCharacteristic(
                      CHARACTERISTIC_UUID,
                      BLECharacteristic::PROPERTY_READ   |
                      BLECharacteristic::PROPERTY_WRITE  |
                      BLECharacteristic::PROPERTY_NOTIFY |
                      BLECharacteristic::PROPERTY_INDICATE
                    );
  //Serverコールバック関数の登録
  pServer->setCallbacks(new MyServerCallbacks());
  //Characteristicコールバック関数の登録
  pCharacteristic->setCallbacks(new MyCallbacks());
  
  //Pearing用コールバック関数の登録
  BLEDevice::setSecurityCallbacks(new MySecurity());   
  //Pearing設定
  BLESecurity *pSecurity = new BLESecurity();
  pSecurity->setKeySize(16);

  pSecurity->setAuthenticationMode(ESP_LE_AUTH_BOND);
  pSecurity->setCapability(ESP_IO_CAP_OUT);
  pSecurity->setInitEncryptionKey(ESP_BLE_ENC_KEY_MASK | ESP_BLE_ID_KEY_MASK);

  uint32_t passkey = BLE_PASSKEY;
  esp_ble_gap_set_security_param(ESP_BLE_SM_SET_STATIC_PASSKEY, &passkey, sizeof(uint32_t));
  
  // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
  // Create a BLE Descriptor
  pCharacteristic->addDescriptor(new BLE2902());

  // serviceの開始
  pService->start();
  // advertisingの取得
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  //advertisingの開始
  BLEDevice::startAdvertising();
  
  Serial.println("Hello World"); // シリアル出力
  M5.Lcd.setRotation(1);//画面回転角(0~3)
  M5.Lcd.setTextFont(4); // フォントの指定
  M5.Lcd.setCursor(10, 120); // カーソル位置の指定
  M5.Lcd.print("START!!"); // Hello Worldのディスプレイ表示

  pinMode(PIR_PIN,INPUT_PULLUP);//PIRの出力ピンを設定

  M5.Speaker.setVolume(10);

  SDfonts.init(SD_PN);
  Serial.println(F("sdfonts liblary"));

  fontDump(50, 10, "Bluetooth通信を行います", 16);
  fontDump(50, 40, "Windowsと接続します", 20);
  fontDump(50, 80, "M5StauckBle!!", 24);


}

void loop() {
  String strSend;//送信文字列
  M5.update();  // ボタン状態更新
  //LEDランプによるステータス表示
  switch(bleState){
    case connect://青色常時点灯
      M5.Lcd.setCursor(10, 120); // カーソル位置の指定
      //printEfont("接続しました!", 0, 16*1); 
      M5.Lcd.clear(BLACK);//Lcd画面消去
      M5.Lcd.print("Connect!!"); // Hello Worldのディスプレイ表示
      break;
    case disconnect://青色点滅
      M5.Lcd.setCursor(10, 120); // カーソル位置の指定
      //printEfont("切断しました!", 0, 16*1); 
      M5.Lcd.clear(BLACK);//Lcd画面消去
      M5.Lcd.print("DisConnect!!"); // Hello Worldのディスプレイ表示
      break;
    case ASAP:
      M5.Lcd.setCursor(10, 120); // カーソル位置の指定
      M5.Lcd.print("ASAP"); // Hello Worldのディスプレイ表示
      break;
    case WAIT:
      M5.Lcd.setCursor(10, 120); // カーソル位置の指定
      M5.Lcd.print("WAIT"); // Hello Worldのディスプレイ表示
      break;
    case WRONG:
      M5.Lcd.setCursor(10, 120); // カーソル位置の指定
      M5.Lcd.print("WRONG"); // Hello Worldのディスプレイ表示
      break;
  }
  // disconnecting
  if (!deviceConnected && oldDeviceConnected) {
    delay(500); // give the bluetooth stack the chance to get things ready
    pServer->startAdvertising(); // restart advertising
    Serial.println("close connection and restart advertising");
    oldDeviceConnected = deviceConnected;
  }
  // connecting
  if (deviceConnected && !oldDeviceConnected) {
    // do stuff here on connecting
    oldDeviceConnected = deviceConnected;
  }

  M5.update();  // ボタン状態更新
  if (deviceConnected) {
    if (M5.BtnA.isPressed()) {
      if (beforeStateSwitch == LOW) {//スイッチがOff→onにてデータを送る
        Serial.println("PUSH_BUTTON ON");
        strSend = "PUSH_ON";
        pCharacteristic->setValue(strSend.c_str());
        pCharacteristic->notify();
        beforeStateSwitch = HIGH;
      }
    } else {
      if (beforeStateSwitch == HIGH) {//スイッチがon→Offにてデータを送る
        M5.Speaker.tone(659, 200);
        delay(200);
        M5.Speaker.tone(523, 200);
        Serial.println("PUSH_BUTTON OFF");
        strSend = "PUSH_OFF";
        pCharacteristic->setValue(strSend.c_str());
        pCharacteristic->notify();
        beforeStateSwitch = LOW;
      }
    }
  }else{
  }

  if(M5.BtnC.wasPressed()) {
    M5.Speaker.tone(440, 100);
    delay(100);
    M5.Speaker.mute();
    delay(100);
    M5.Speaker.tone(440, 100);
    delay(100);
    M5.Speaker.mute();
    delay(100);
    M5.Speaker.tone(440, 100);

      int key=BLE_PASSKEY;
      uint8_t* valueBleKey=(uint8_t*)(&key);
      
      Serial.println("PushButtonC Pearing!!");
      pCharacteristic->setValue(valueBleKey, 4);
      pCharacteristic->notify();
  }

  if(digitalRead(PIR_PIN)){
    M5.Lcd.setCursor(10, 100); // カーソル位置の指定
    //printEfont("切断しました!", 0, 16*1); 
    M5.Lcd.clear(BLACK);//Lcd画面消去
    M5.Lcd.print("PIR ON!!"); // Hello Worldのディスプレイ表示
  }else{
    M5.Lcd.setCursor(10, 100); // カーソル位置の指定
    //printEfont("切断しました!", 0, 16*1); 
    M5.Lcd.clear(BLACK);//Lcd画面消去
    M5.Lcd.print("PIR OFF!!"); // Hello Worldのディスプレイ表示
  }
}
