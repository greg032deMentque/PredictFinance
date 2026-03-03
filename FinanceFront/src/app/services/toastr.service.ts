import { Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class ToastService {

  constructor(private toastr: ToastrService,) { }

  public error(message: string) {
    console.log(message)
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
        message = "Aucune erreur n'a été levée."
      }
    }
    this.toastr.success(message, "Succès", { timeOut: 4500 })
  }

  public warning(message: string) {
    if(message != null){
      if (message.length > 160) {
        message = "Veuillez vérifier tous les paramètres."
      }
    }
    this.toastr.warning(message, "Attention", { timeOut: 4500 })
  }
}
