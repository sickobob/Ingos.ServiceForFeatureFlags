import {HttpClient} from '@angular/common/http';
import {Component, OnInit} from '@angular/core';
import {Setting, SettingsService} from './services/settings.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  settings: Setting[] = []; // общий массив который поступает с бэка
  settingsTypeString: Setting[] = []; // карточка натсроек
  settingsTypeFt: Setting[] = []; //toggles
  activeTab: string = 'Table1'; // для переключения вкладок
  inputTable1: string = '';// для поиска
  inputTable2: string = '';// для поиска
  filteredSetting: Setting | undefined;//
  isSearchTab: boolean = false; // флаг для поиска
  settingsToUpdate: Setting[] = [];
  dbNames: string[] = []; // имена бд
  isConnected: boolean = false;
  selectedDatabase: string = ''; // Выбранная база данных
  customConnectionString: string = ''; // Своя строка подключения
  isCustomDatabase: boolean = false; // Флаг для отображения поля ввода
  defaultSetting: Setting = { code: '', description: 'No matching setting', status: false, type:'', intValue:2, boolValue:false, name:'', stringValue:''};// отбойник, чтобы не было исключения для filteredSetting :undf
  constructor(private http: HttpClient, protected SettingsService: SettingsService) {
    this.filteredSetting = this.defaultSetting;
  }

  ngOnInit() {
    this.getDatabaseNames();
    if(sessionStorage.getItem('isConnected')) {
      this.getAllSettings();
      this.selectedDatabase = sessionStorage.getItem('selectedDatabase')!;
      this.isConnected = true;
    }
  }
  onDatabaseChange() {
    this.isCustomDatabase = this.selectedDatabase === 'custom';
  }

  getAllSettings() {
    if(this.isConnected && this.selectedDatabase !== sessionStorage.getItem('selectedDatabase')) {
      this.settings=[];
      this.settingsTypeFt=[];
      this.settingsTypeString=[];
    }
    this.SettingsService.getAllSettings().subscribe({
      next: (data) => {
        this.settings = data;
        this.filterSettingsByType();
      },
      error: (err) => console.error('Ошибка:', err),
    });
  }
// загрузка по типу
  filterSettings() {
    this.settingsTypeString = this.settings.filter(
      (setting) => setting.type === 'string'
    );
    this.settingsTypeFt = this.settings.filter(
      (setting) => setting.type === 'ft'
    );
  }
  // поиск
  search(searchCode: string, settingsArray: Setting[]) {
    if (!searchCode.trim()) {
      this.filteredSetting = undefined;
    }
    this.isSearchTab = true;
    this.filteredSetting= settingsArray.find(
      (setting) => setting.code === searchCode.trim()
    );

    return this.filteredSetting;
  }
//переключение вкладок
  openTab(tabName: string) {
    this.activeTab = tabName;
    this.isSearchTab = false;
    this.filteredSetting = this.defaultSetting;
  }

// Загрузка всех настроек без определения типа, просто зная, что есть два типа значений поля type
  filterSettingsByType() {
    this.settings.forEach(setting => {
      if (this.settingsTypeString.length === 0 || this.settingsTypeString[0].type === setting.type) {
        this.settingsTypeString.push(setting);
      } else {
        this.settingsTypeFt.push(setting);
      }
    });

  }
//сохранение
  saveSetting(setting: Setting) {
    if(!setting) {
      return;
    }
    this.SettingsService.updateSetting(setting);
    const oldSetting = this.settingsTypeFt.find((oldSetting) => oldSetting.code === setting.code);
    if (oldSetting) {
      oldSetting.status = setting.status;
    }
  }
  //получение названий бд
  getDatabaseNames() {
    this.SettingsService.getAllDbNames().subscribe({
      next: (data) => {
        this.dbNames = data;
      },
      error: (err) => {
        console.error('Ошибка при загрузке имён баз данных:', err);
      },
    });
  }
  // подключение к бд
  connect() {
    if (!this.selectedDatabase) {
      alert('Введите строку подключения.');
      return;
    }
    if(this.isConnected && this.selectedDatabase === sessionStorage.getItem('selectedDatabase')) {
      alert('Вы уже подключены к этой базе.');
      return;
    }
    if(this.isConnected && this.selectedDatabase !== sessionStorage.getItem('selectedDatabase')) {
      sessionStorage.removeItem('isConnected');
      sessionStorage.removeItem('selectedDatabase');
    }
    this.SettingsService.connectToDataBase(this.selectedDatabase).subscribe({
      next: () => {
        console.log('Подключение успешно установлено.');
      },
      error: (err) => {
        console.error('Ошибка при подключении:', err);
      }
    });
    this.isConnected = true;
    this.getAllSettings();
    sessionStorage.setItem('isConnected','true');
    sessionStorage.setItem('selectedDatabase',this.selectedDatabase);
  }
  onCheckboxChange(setting: Setting) {
    const index = this.settingsToUpdate.findIndex(s => s.code === setting.code);

    if (index === -1) {
      this.settingsToUpdate.push(setting);
    } else {
      this.settingsToUpdate[index] = setting;
    }

    console.log('Обновлённые настройки:', this.settingsToUpdate);
  }

}
