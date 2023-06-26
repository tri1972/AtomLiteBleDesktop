#include <BLEDevice.h>
#include <BLEServer.h>
//#include <BLEUtils.h>
#include <BLE2902.h>
#include <M5Stack.h>
//#include "efont.h"                                          // Unicode表示用ライブラリ
//#include "efontESP32.h"                                     // Unicode表示用ライブラリ　
//#include "efontEnableJa.h"                                  //漢字すべて(サイズが大きすぎる)
//#include "efontEnableJaMini.h"                              //常用漢字＋α

/*定義部分*/
#define DEVICE_NAME "M5STACKTRI" //Bluetoothデバイス名設定
#define SERVICE_UUID        "ee007086-0dc9-4a48-b381-0f9e56d8c597"
#define CHARACTERISTIC_UUID "245c84dc-9422-41fb-bbf9-ddcd7da28120"
#define BLE_PASSKEY 123456 //ペアリング時のパスコード値設定

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

int volume = 1;

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

  M5.Speaker.setVolume(10);
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


    // notify changed value

  if (deviceConnected) {
    if(M5.BtnA.wasPressed()) {
      M5.Speaker.tone(659, 200);
      delay(200);
      M5.Speaker.tone(523, 200);
      strSend = "PIR_ON";
    }else{
      strSend = "PIR_OFF";
    }
    pCharacteristic->setValue(strSend.c_str());
    pCharacteristic->notify();
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
  }

}
