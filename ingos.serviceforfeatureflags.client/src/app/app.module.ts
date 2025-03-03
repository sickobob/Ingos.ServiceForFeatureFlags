import {HttpClientModule} from '@angular/common/http';
import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import { SettingDetailComponent } from './components/setting-detail/setting-detail.component';
import { SettingFormComponent } from './components/setting-form/setting-form.component';
import {NgIf} from "@angular/common";
import {DatePipe } from "@angular/common";

@NgModule({
  declarations: [
    AppComponent,
    SettingDetailComponent,
    SettingFormComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,FormsModule, NgIf,DatePipe
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
}
