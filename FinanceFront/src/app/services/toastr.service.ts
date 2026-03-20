import { Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class ToastService {

  constructor(private toastr: ToastrService,) { }

  public error(message: string) {
    if(message != null){
      if (message.length > 160) {
        message = "Une erreur est survenue."
      }
    }
    this.toastr.error(message, "Erreur", { timeOut: 4500 })
  }


  public success(message: string) {
    if(message != null){
      if (message.length > 160) {
        message = "Aucune erreur n'a Ã©tÃ© levÃ©e."
      }
    }
    this.toastr.success(message, "SuccÃ¨s", { timeOut: 4500 })
  }

  public warning(message: string) {
    if(message != null){
      if (message.length > 160) {
        message = "Veuillez vÃ©rifier tous les paramÃ¨tres."
      }
    }
    this.toastr.warning(message, "Attention", { timeOut: 4500 })
  }
}
