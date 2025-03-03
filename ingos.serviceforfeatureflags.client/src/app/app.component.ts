import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import {SettingsService} from './services/settings.service';
import {Setting} from './services/settings.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  public settings: Setting[] = [];
  public toggles: Setting[] = [];
  activeTab: string = 'Table1';
  inputTable1: string = '';
  inputTable2: string = '';
  filteredSetting: Setting | undefined ;
  isSearchTab1: boolean = false;
  isSearchTab2: boolean = false;
  constructor(private http: HttpClient, protected SettingsService: SettingsService) { }

  ngOnInit() {
this.getAllSettings();
  }
  getAllSettings() {
    this.SettingsService.getAllSettings().subscribe({
      next: (data) => this.settings = data,
      error: (err) => console.error('Ошибка:', err)
    });
  }
  filterToggles() {
    this.toggles = this.settings.filter(setting => setting.type);
  }

  searchTable1() {
    if (this.inputTable1) {
      this.SettingsService.getSetting(this.inputTable1,'string').subscribe({
        next: (data) => this.filteredSetting = data,
        error: (err) => console.error('Ошибка:', err)
      });
      if (this.filteredSetting != undefined) {
        this.isSearchTab1 = true;
      }
    } else {
     this.getAllSettings();
      this.isSearchTab1 = false;
    }
  }
  searchTable2() {
    if (this.inputTable1) {
      this.SettingsService.getSetting(this.inputTable1,'string').subscribe({
        next: (data) => this.filteredSetting = data,
        error: (err) => console.error('Ошибка:', err)
      });
      if (this.filteredSetting != undefined) {
        this.isSearchTab1 = true;
      }
    } else {
      this.getAllSettings();
      this.isSearchTab1 = false;
    }
  }
  openTab(tabName: string) {
    this.activeTab = tabName;
    this.isSearchTab1 = false;
  }

  // Обработка кнопки Connect
  connect() {
    alert('Подключение к базе данных...');
  }
   RemoveElementFromSettingsArray(key: string) {
    this.settings.forEach((value,index)=>{
      if(value.code==key) this.settings.splice(index,1);
    });
  }
  title = 'ingos.serviceforfeatureflags.client';
  protected readonly Set = Set;
}
